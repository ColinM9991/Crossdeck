# .NET skill reference (dotnet/skills)

Source: `.opencode/skills/dotnet-skills`, a git submodule of https://github.com/dotnet/skills (branch `main`, recursively cloned).

Read this file when a .NET activity below is in progress and a skill must be chosen. The skill ID is the skill folder name; load it with the `skill` tool. Load only the skill(s) that match the current activity, at most one or two per task, and never for unrelated work.

## dotnet

General C#/.NET development: C# specialist routing, safe C# refactoring, breaking-change checks, local SDK setup, MSBuild basics.

| Skill | Purpose |
| --- | --- |
| `csharp-expert` | Route C# and .NET requests to the exact installed specialist or smallest dotnet/skills marketplace plugin. USE FOR: "which specialist should own this" or "how do I add the skill" requests involving ASP.NET Core endpoints, Blazor, MAUI... |
| `csharp-refactoring` | Safely refactors C#/.NET code without changing behavior. USE FOR any request to refactor, rename, move, extract, inline, merge, consolidate, deduplicate, split, or modernize C# code, even a small extract, inline, or rename, or merging... |
| `dotnet-breaking-changes` | Use before changing .NET/C# code whose callers you do not control: a NuGet library, or a repo with PublicAPI.Shipped/Unshipped.txt, ApiCompat, multiple TargetFrameworks, InternalsVisibleTo, or generated (.g.cs) partials. Green builds miss... |
| `msbuild` | Use before MSBuild custom-target or incremental-generation repairs, XML/import/hook review, .binlog capture/replay, or assessing supported/unsupported build-speedup claims and controlled timings, even without a checkout. For MSBuild... |
| `setup-local-sdk` | Install a .NET SDK locally for safe preview testing, specific-version pinning, or reproducible team setups — without modifying the system-wide installation. USE FOR: trying .NET previews safely, testing specific SDK versions, installing... |

## dotnet-advanced

C# file-based scripts, P/Invoke native interop, NuGet trusted publishing, SIMD vectorization.

| Skill | Purpose |
| --- | --- |
| `csharp-scripts` | Run file-based C# apps with the .NET CLI when the user explicitly wants C#/.NET code without creating a project. Use for C# language/API experiments, one-file C# apps, small multi-file C# apps composed with `#:include`/`#:exclude`, or C#... |
| `dotnet-pinvoke` | Correctly call native (C/C++) libraries from .NET using P/Invoke and LibraryImport. Covers function signatures, string marshalling, memory lifetime, SafeHandle, and cross-platform patterns. USE FOR: writing new P/Invoke or LibraryImport... |
| `nuget-trusted-publishing` | Set up NuGet trusted publishing (OIDC) on a GitHub Actions repo — replaces long-lived API keys with short-lived tokens. USE FOR: trusted publishing, NuGet OIDC, keyless NuGet publish, migrate from NuGet API key, NuGet/login, secure NuGet... |
| `vectorization` | Design, implement, optimize, and review SIMD code in .NET. USE FOR: vectorizing scalar loops with TensorPrimitives, Vector64/128/256/512, or platform hardware intrinsics; reviewing existing SIMD code, including the generic Vector type, for... |

## dotnet-msbuild

MSBuild project and build file authoring, build failure and performance diagnosis, binlogs, incremental builds.

| Skill | Purpose |
| --- | --- |
| `binlog-failure-analysis` | Analyze MSBuild binary logs to diagnose build failures. USE FOR: build errors that are unclear from console output, diagnosing cascading failures across multi-project builds, tracing MSBuild target execution order, and generally any... |
| `binlog-generation` | Generate MSBuild binary logs (binlogs) for build diagnostics and analysis. USE FOR: adding /bl:{} to any dotnet build, test, pack, publish, or restore command to capture a full build execution trace, prerequisite for... |
| `build-parallelism` | Analyze MSBuild scheduling across at least two distinct projects in a solution/filter/Build.proj. USE FOR: measured idle workers, -m not scaling, serial ProjectReference critical paths, graph builds, MSBuild Projects-list scheduling. DO... |
| `build-perf-baseline` | Establish MSBuild/.NET baselines before optimizing. USE FOR: unranked slow builds; cold/warm/incremental/no-op measurements; before/after comparisons; CI caching; static graph, artifacts output, dependency graph trimming. DO NOT USE FOR:... |
| `build-perf-diagnostics` | Diagnose MSBuild bottlenecks from binlog timelines and Target/Task Performance Summaries after baseline measurements. USE FOR: unknown build slowness, RAR >5s, analyzers >30% of Csc, a target >50% of build, node utilization below 80%,... |
| `check-bin-obj-clash` | Find OutputPath/IntermediateOutputPath collisions between distinct projects or required property/TFM variants. USE FOR: missing/overwritten bin/obj or project.assets.json, parallel 'Cannot create a file when that file already exists' or... |
| `copy-to-output-directory` | Choosing an MSBuild CopyToOutputDirectory / CopyToPublishDirectory mode: Never, PreserveNewest, Always, and IfDifferent (MSBuild 17.13+), plus $(SkipUnchangedFilesOnCopyAlways). USE FOR: removing the per-build Always copy perf hit;... |
| `directory-build-organization` | USE ONLY for (1) two or more projects with repeated MSBuild policy, targets, or package versions, or (2) an existing Directory.Build.props/targets/rsp hierarchy with an import, placement, or layering defect. A one-project repo without an... |
| `eliminate-redundant-restores` | Remove repeated NuGet restores from multi-step .NET builds safely. USE FOR: build/test/publish steps that each restore, Directory.Build.rsp with -restore, safe restore suppression, NETSDK1004/NETSDK1047 after --no-restore. DO NOT USE FOR:... |
| `eval-performance` | Diagnose measured MSBuild evaluation cost before targets run. USE FOR: evaluation-only repeats under global-property variants, expensive node_modules/.git/bin/obj globs, import depth >20, /pp >10K lines, file-I/O property functions,... |
| `extension-points` | Own MSBuild import and hook discovery. USE FOR: CustomBefore/CustomAfter hooks, ordered wildcard and NuGet auto-imports, control properties, build/buildTransitive packed layout, package ID and file-name matching, per-TFM forwarders, and... |
| `find-repeated-work-within-one-build` | Use ONLY for auditing executed-task counts or batching/hoisting costs within ONE MSBuild invocation: per-project Exec, duplicate dependency builds, per-item generators, run-once races. NEVER load for evaluation-only global-property... |
| `including-generated-files` | Own MSBuild generated-artifact integration. USE FOR: a target that creates or should create source, Content, None, or another physical file but the artifact is missing from compilation or output; target timing; evaluation-time glob misses;... |
| `incremental-build` | Fix MSBuild incremental/no-op builds: 'nothing changed but it rebuilds anyway'. USE FOR: targets re-executing on subsequent builds, missing Inputs/Outputs or FileWrites, volatile output paths, untracked writes, glob changes, Visual Studio... |
| `item-management` | Own concrete MSBuild ItemGroup and item-expression questions. USE FOR: Include, Remove, Update, item identity and metadata, transforms, filtering, batching, duplicate or overlapping items, and reviews that verify whether those operations... |
| `msbuild-antipatterns` | DO NOT INVOKE when the primary request explicitly asks to convert, migrate, modernize, or rewrite a legacy/old-style project to SDK style; use msbuild-modernization. Migration prompts often mention ToolsVersion, explicit Compile/Reference... |
| `msbuild-modernization` | Guide for modernizing and migrating MSBuild project files to SDK-style format. USE FOR: converting legacy .csproj/.vbproj with verbose XML to SDK-style, migrating packages.config to PackageReference, removing Properties/AssemblyInfo.cs in... |
| `property-patterns` | Diagnose and fix concrete MSBuild property defects in projects and existing shared-file hierarchies. USE FOR: conditions, defaults, append versus overwrite, last-write-wins values, OS/TFM checks, portable paths, normalization, and reviews... |
| `resolve-project-references` | Guide for interpreting ResolveProjectReferences time in MSBuild performance summaries. Activate when ResolveProjectReferences appears as the most expensive target and developers are trying to optimize it directly. Explains that the... |
| `target-authoring` | Canonical patterns for writing custom MSBuild targets. USE FOR: diagnosing and fixing custom target authoring anti-patterns; broken SDK target chains across files (e.g., Directory.Build.targets silently redefining SDK targets); targets... |
| `troubleshoot-binlog-mcp` | Diagnose dotnet-msbuild binlog MCP startup failures. USE FOR: CONNECTION_CLOSED, binlog server won't connect, missing binlog tools, Microsoft.AITools.BinlogMcp not found, dnx launch errors. Checks workspace SDK, feeds, and source mapping... |

## dotnet-diag

Performance analysis, crash and dump analysis, tracing, microbenchmarking.

| Skill | Purpose |
| --- | --- |
| `analyzing-dotnet-performance` | Scans .NET code for ~50 performance anti-patterns across async, memory, strings, collections, LINQ, regex, serialization, and I/O with tiered severity classification. Use when analyzing .NET code for optimization opportunities, reviewing... |
| `android-tombstone-symbolication` | Symbolicate the .NET runtime frames in an Android tombstone file. Extracts BuildIds and PC offsets from the native backtrace, downloads debug symbols from the Microsoft symbol server, and runs llvm-symbolizer to produce function names with... |
| `apple-crash-symbolication` | Symbolicate .NET runtime frames in Apple platform .ips crash logs (iOS, tvOS, Mac Catalyst, macOS). Extracts UUIDs and addresses from the native backtrace, locates dSYM debug symbols, and runs atos to produce function names with source... |
| `clr-activation-debugging` | Diagnoses .NET Framework CLR activation issues using CLR activation logs (CLRLoad logs) produced by mscoree.dll. Use when: the shim picks the wrong runtime, fails to load any runtime, shows unexpected .NET 3.5 Feature-on-Demand (FOD)... |
| `dotnet-trace-collect` | Guide developers through capturing diagnostic artifacts to diagnose production .NET performance issues. Use when the user needs help choosing diagnostic tools, collecting performance data, or understanding tool trade-offs across different... |
| `dump-collect` | Configure and collect crash dumps for modern .NET applications. USE FOR: enabling automatic crash dumps for CoreCLR or NativeAOT, capturing dumps from running .NET processes, setting up dump collection in Docker or Kubernetes, using... |
| `microbenchmarking` | Activate this skill when BenchmarkDotNet (BDN) is involved in the task — creating, running, configuring, or reviewing BDN benchmarks. Also activate when microbenchmarking .NET code would be useful and BenchmarkDotNet is the likely tool.... |

## dotnet-test

Running, writing, reviewing, auditing, and migrating .NET tests.

| Skill | Purpose |
| --- | --- |
| `assertion-quality` | Analyze assertion quality, depth, variety, and false confidence in existing tests. ALWAYS USE when asked about weak, shallow, trivial, always-true, self-referential, assertion-free, presence/truthiness-only, or insufficiently diverse... |
| `code-testing` | ALWAYS USE for test work that requires changes: write, add, generate, repair, or strengthen tests for existing code in xUnit, MSTest, NUnit, pytest, Vitest/Jest, Go, or another framework. Includes regression cases, failing or flaky tests,... |
| `code-testing-agent` | Legacy compatibility alias for the code-testing skill. Do not self-activate for user requests and do not treat this name as a custom agent. When invoked explicitly by older instructions, load code-testing and follow it instead. |
| `code-testing-extensions` | Provides file paths to language-specific extension files for the code-testing pipeline. Read this reference-only catalog for extension guidance files (e.g., dotnet.md for .NET, cpp.md for C++). Agents and skills read its bundled files... |
| `coverage-analysis` | Activation requires either supplied .NET coverage reports/percentages/line, branch, or condition metrics, or an explicit request to collect .NET coverage for analysis. USE FOR: interpreting that evidence, including Cobertura data, partial... |
| `crap-score` | Calculates CRAP (Change Risk Anti-Patterns) for a named .NET method, class, or file. USE FOR: explicit CRAP calculation or coverage-and-complexity risk within that named target, including which tests to prioritize. DO NOT USE FOR:... |
| `detect-static-dependencies` | ACTIVATION PREREQUISITE: the request or discovered target must explicitly identify C#, .NET, `.cs`, or `.csproj`; otherwise stay dormant without invoking this skill. USE FOR: locating System.DateTime.Now/UtcNow, System.IO.File/Directory,... |
| `filter-syntax` | Reference-only filter syntax for VSTest and MTP with MSTest, NUnit, xUnit v3, and TUnit. Load only after the platform/framework is known and a consumer needs to create or translate a filter. Do not load for unfiltered runs or platform... |
| `find-untested-sources` | MANDATORY for static source-to-test pairing: find or list source files/modules without corresponding tests, or suggest test locations from repository structure. Invoke even for a tiny package; do not substitute manual globbing. Uses Roslyn... |
| `generate-testability-wrappers` | DO NOT USE when the target already consumes an injected interface or built-in abstraction such as IFileSystem or TimeProvider, even if the request says "generate a wrapper"; no new wrapper is needed. Use only when C# source calls an... |
| `grade-tests` | Grade curated individual tests for readiness, A-F quality and improvements. ALWAYS USE FOR: grade tests, review only a named test, per-test readiness or quality bands for supplied methods, bodies, file spans or bounded PR diffs, including... |
| `migrate-static-to-wrapper` | ALWAYS USE when asked to migrate, replace, or make testable existing C# static calls with a named wrapper or built-in abstraction: DateTime.UtcNow/Now or DateTimeOffset.UtcNow to TimeProvider/IClock, File.* to IFileSystem or an existing... |
| `mtp-hot-reload` | Set up or recover MTP hot reload for a long-lived console-host edit/re-run loop in a Microsoft Testing Platform project. Use for explicit MTP console requests such as "enable hot reload", "dotnet run to rerun on edit", or unsupported/rude... |
| `platform-detection` | Identify a .NET project's test platform, framework, command mode, and SDK-style vs classic project system. Use only for "which test platform/framework?", "VSTest or MTP?", or "what runner does this project use?", including bridge settings,... |
| `run-tests` | ALWAYS USE before running .NET tests or answering with a test command or flags. Trigger on "run the tests", "exact dotnet test command", one test/class/category/trait/target framework, combined filters, `--filter-query`, `--no-build`,... |
| `scaffold-dotnet-test-project` | MUST USE when an existing .NET test project was excluded from a .slnf/CI solution filter, disappeared from .sln/.slnx discovery, or lost its production ProjectReference; also for requests to set up, create, reuse, add, register, include,... |
| `test-analysis-extensions` | Provides file paths to language-specific reference files for the test ANALYSIS skills (assertion-quality, test-anti-patterns, test-gap-analysis, test-smell-detection, test-tagging). Read this catalog to discover available extension files... |
| `test-anti-patterns` | Audit a test file or suite; produce a severity-ranked diagnostic report. ALWAYS USE for tests that verify nothing, missing/tautological assertions, swallowed/broad exceptions, flaky/order-dependent tests, duplication, or magic values.... |
| `test-gap-analysis` | Find caller-visible production changes existing tests would miss through pseudo-mutation analysis; verify candidates or close gaps only when requested. Includes read-only per-test composition for grading. Polyglot. USE FOR: would tests... |
| `test-smell-detection` | Audits existing tests in any language using formal, research-backed test smell names and the testsmells.org 19-smell academic taxonomy. Use when the caller asks for an academic or citable test-smell review, named smell categories, or a... |
| `test-tagging` | Classifies existing tests by standard traits and reports their distribution. USE FOR: tagging all tests with category attributes, categorizing/tagging/ labeling each test, compare happy vs error paths, audit the test mix, describe coverage... |
| `test-value-evidence` | Assess tests for an identified code or test change, feature, PR or commit. USE FOR: review changed behavior or regression tests, prove pre-fix/missing behavior is detected, revision-bound red-green evidence. Polyglot. DO NOT USE without an... |
| `testability-obstacle` | MUST USE for C#/.NET deterministic tests that require the smallest production seam for DateTime/Task.Delay/File/Environment/Guid/Random, static API preservation, nested/parallel overrides, or no real I/O. USE ONLY when the target workspace... |
| `writing-mstest-tests` | ALWAYS USE when asked to fix, rewrite, update, improve, modernize, show corrected code for, or explain existing MSTest tests or MSTest-specific configuration. Use for "review" when corrected code or edits are wanted, even for one pasted... |

## dotnet-nuget

NuGet Central Package Management.

| Skill | Purpose |
| --- | --- |
| `convert-to-cpm` | Convert .NET projects and solutions (.sln, .slnx) to NuGet Central Package Management (CPM) using Directory.Packages.props. USE FOR: converting to CPM, centralizing or aligning NuGet package versions across multiple projects, inlining... |

## dotnet-upgrade

Framework version upgrades, nullable reference types, Thread.Abort migration, Native AOT compatibility.

| Skill | Purpose |
| --- | --- |
| `dotnet-aot-compat` | Make .NET projects compatible with Native AOT and trimming by systematically resolving IL trim/AOT analyzer warnings. USE FOR: making projects AOT-compatible, fixing trimming warnings, resolving IL warnings (IL2026, IL2070, IL2067, IL2072,... |
| `migrate-dotnet10-to-dotnet11` | Migrate a .NET 10 project or solution to .NET 11 and resolve all breaking changes. This is a MIGRATION skill — use it when upgrading from .NET 10 to .NET 11, NOT for writing new programs. USE FOR: upgrading TargetFramework from net10.0 to... |
| `migrate-dotnet8-to-dotnet9` | Migrate a .NET 8 project to .NET 9 and resolve all breaking changes. USE FOR: upgrading TargetFramework from net8.0 to net9.0, fixing build errors after updating the .NET 9 SDK, resolving behavioral changes in .NET 9 / C# 13 / ASP.NET Core... |
| `migrate-dotnet9-to-dotnet10` | Migrate a .NET 9 project or solution to .NET 10 and resolve all breaking changes. USE FOR: upgrading TargetFramework from net9.0 to net10.0, fixing build errors after updating the .NET 10 SDK, resolving source and behavioral changes in... |
| `migrate-nullable-references` | Enable nullable reference types in a C# project and systematically resolve all warnings. USE FOR: adopting NRTs in existing codebases, file-by-file or project-wide migration, fixing CS8602/CS8618/CS86xx warnings, annotating APIs for... |
| `thread-abort-migration` | Guides migration of .NET Framework Thread.Abort usage to cooperative cancellation in modern .NET. USE FOR: modernizing code that calls Thread.Abort, catching ThreadAbortException, replacing Thread.ResetAbort, replacing Thread.Interrupt for... |

## Keeping the permissions and this file in sync

1. `git submodule update --remote --recurse-submodules .opencode/skills/dotnet-skills`
2. `pwsh ./sync-dotnet-skills.ps1`

Step 2 regenerates the `permissions` array in `.opencode/opencode.json` (allow every skill of the plugins in `sync-dotnet-skills.ps1`, deny every other skill that exists in the submodule checkout) and this file. Skills from other sources are not touched. To enable or disable a plugin, edit the `$plugins` list in `sync-dotnet-skills.ps1` and rerun it.

