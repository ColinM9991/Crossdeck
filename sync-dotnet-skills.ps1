# Syncs the dotnet/skills submodule with this repo's OpenCode config and docs.
# 1. Regenerates the `permissions` skill rules in .opencode/opencode.json:
#    allow every skill of the enabled plugins, deny every other skill that
#    exists in the submodule checkout (other plugins, meta skills, test
#    fixtures). Skills from other sources (built-in, global, project) are
#    untouched.
# 2. Regenerates agents/dotnet-skills.md (activity -> plugin -> skill table).
# Path-independent; run: pwsh ./sync-dotnet-skills.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$subRoot = Join-Path $root '.opencode\skills\dotnet-skills'
$sub = Join-Path $subRoot 'plugins'

# The seven dotnet/skills plugins this project imports. Keep in sync with
# agents/dotnet-skills.md.
$plugins = @('dotnet', 'dotnet-advanced', 'dotnet-msbuild', 'dotnet-diag', 'dotnet-test', 'dotnet-nuget', 'dotnet-upgrade')

$pluginNotes = [ordered]@{
    'dotnet'           = 'General C#/.NET development: C# specialist routing, safe C# refactoring, breaking-change checks, local SDK setup, MSBuild basics.'
    'dotnet-advanced'  = 'C# file-based scripts, P/Invoke native interop, NuGet trusted publishing, SIMD vectorization.'
    'dotnet-msbuild'   = 'MSBuild project and build file authoring, build failure and performance diagnosis, binlogs, incremental builds.'
    'dotnet-diag'      = 'Performance analysis, crash and dump analysis, tracing, microbenchmarking.'
    'dotnet-test'      = 'Running, writing, reviewing, auditing, and migrating .NET tests.'
    'dotnet-nuget'     = 'NuGet Central Package Management.'
    'dotnet-upgrade'   = 'Framework version upgrades, nullable reference types, Thread.Abort migration, Native AOT compatibility.'
}

function Get-SkillPurpose([string]$skillDir) {
    $lines = Get-Content (Join-Path $skillDir 'SKILL.md')
    $start = -1
    $inline = ''
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^description:\s*(.*)$') { $start = $i; $inline = $Matches[1].Trim(); break }
    }
    if ($start -lt 0) { return '(no description)' }
    $parts = @()
    if ($inline -match '^[>|][+-]?\s*$') {
        $j = $start + 1
        while ($j -lt $lines.Count -and $lines[$j] -match '^\s+\S') { $parts += $lines[$j].Trim(); $j++ }
    }
    elseif ($inline -ne '') { $parts += $inline }
    $full = ($parts -join ' ').Trim()
    if ($full.Length -ge 2 -and $full[0] -eq $full[$full.Length - 1] -and ($full[0] -eq '"' -or $full[0] -eq "'")) {
        $full = $full.Substring(1, $full.Length - 2).Trim()
    }
    if ($full.Length -le 240) { return $full }
    $cut = $full.Substring(0, 240)
    $sp = $cut.LastIndexOf(' ')
    if ($sp -gt 160) { $cut = $cut.Substring(0, $sp) }
    return $cut + '...'
}

# --- 1. permissions rules in .opencode/opencode.json -------------------------
$owner = @{}
foreach ($p in $plugins) {
    Get-ChildItem (Join-Path $sub "$p\skills") -Directory | ForEach-Object {
        if ($owner.ContainsKey($_.Name)) { throw "Duplicate skill ID across plugins: $($_.Name)" }
        $owner[$_.Name] = $p
    }
}

# Every skill in the checkout that is not in the enabled plugins gets a deny
# rule; skills from other sources are left alone.
$denied = Get-ChildItem $subRoot -Recurse -Force -File -Filter 'SKILL.md' |
    Where-Object { -not $owner.ContainsKey($_.Directory.Name) } |
    ForEach-Object { $_.Directory.Name } |
    Sort-Object -Unique

$rules = @()
foreach ($id in $denied) { $rules += [pscustomobject]@{ action = 'skill'; resource = $id; effect = 'deny' } }
foreach ($id in ($owner.Keys | Sort-Object)) { $rules += [pscustomobject]@{ action = 'skill'; resource = $id; effect = 'allow' } }

$cfgPath = Join-Path $root '.opencode\opencode.json'
$cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
if ($cfg.PSObject.Properties.Name -contains 'permissions') { $cfg.PSObject.Properties.Remove('permissions') }
$cfg | Add-Member -MemberType NoteProperty -Name 'permissions' -Value $rules
($cfg | ConvertTo-Json -Depth 20) | Set-Content -Path $cfgPath -Encoding utf8
Write-Host "Wrote $($owner.Count) allowed and $($denied.Count) denied skill IDs to $cfgPath"

# --- 2. agents/dotnet-skills.md ----------------------------------------------
$doc = @(
    '# .NET skill reference (dotnet/skills)',
    '',
    'Source: `.opencode/skills/dotnet-skills`, a git submodule of https://github.com/dotnet/skills (branch `main`, recursively cloned).',
    '',
    'Read this file when a .NET activity below is in progress and a skill must be chosen. The skill ID is the skill folder name; load it with the `skill` tool. Load only the skill(s) that match the current activity, at most one or two per task, and never for unrelated work.'
)
foreach ($p in $plugins) {
    $doc += '', "## $p", '', $pluginNotes[$p], '', '| Skill | Purpose |', '| --- | --- |'
    Get-ChildItem (Join-Path $sub "$p\skills") -Directory | Sort-Object Name | ForEach-Object {
        $doc += "| ``$($_.Name)`` | $(Get-SkillPurpose $_.FullName) |"
    }
}
$doc += @(
    '',
    '## Keeping the permissions and this file in sync',
    '',
    '1. `git submodule update --remote --recurse-submodules .opencode/skills/dotnet-skills`',
    '2. `pwsh ./sync-dotnet-skills.ps1`',
    '',
    'Step 2 regenerates the `permissions` array in `.opencode/opencode.json` (allow every skill of the plugins in `sync-dotnet-skills.ps1`, deny every other skill that exists in the submodule checkout) and this file. Skills from other sources are not touched. To enable or disable a plugin, edit the `$plugins` list in `sync-dotnet-skills.ps1` and rerun it.'
)
$agentsPath = Join-Path $root 'agents\dotnet-skills.md'
(($doc -join [Environment]::NewLine) + [Environment]::NewLine) | Set-Content -Path $agentsPath -Encoding utf8
Write-Host "Wrote $agentsPath"
