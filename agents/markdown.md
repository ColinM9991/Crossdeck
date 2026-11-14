# Markdown rules

Read before you write or edit a markdown file in this repository (see AGENTS.md). Applies to all `.md` files: plans, docs, agent rules.

## Line breaks

- Markdown treats a single newline inside a paragraph as a space. Consecutive lines with no blank line between them render as one wrapped paragraph. This is the bug behind "the intro is all on one line".
- Two things force a new line:
  - a blank line starts a new paragraph.
  - two spaces at the very end of the line (a hard break) start a new line within the same paragraph.
- Use hard breaks for metadata blocks (Status, Date, Sources) and any line that must stand alone without a paragraph break.
- A backslash at the end of the line also makes a hard break. The two-space form is the house style.
- A single trailing space does nothing. Never leave trailing spaces unless they are the two that make a hard break.
- Never rely on a bare newline: join the lines into one, or add a hard break or a blank line.

## Headings

- One `#` title per document, `##` for sections, `###` for subsections. Never skip a level.
- A blank line before and after each heading, a space after the `#`, and no trailing full stop.

## Emphasis

- `**bold**` for terms of art, `*italic*` sparingly, `~~strikethrough~~` for removed or renamed names.

## Lists

- Unordered lists use `-`, ordered lists use `1.` `2.` `3.`.
- One marker style per list. Nest with two spaces.
- A blank line between prose and a list.
- Consecutive items are a tight list. A blank line between items makes a loose list, where each item becomes a paragraph. Use loose only when an item needs several paragraphs.

## Code

- Inline code in backticks. File paths, type names, members, and commands always go in inline code.
- Fenced blocks open with a language tag (`csharp`, `mermaid`, `powershell`), close on their own line, and have a blank line before and after.
- An unclosed fence swallows the rest of the file. Every fence must be closed.

## Tables

- A header row, a separator row of at least one `-` per column, then one row per line.
- A blank line before and after a table.
- One line per row. No wrapped text inside a cell. Use `:---`, `:--:`, `---:` for left, centre, and right alignment.

## Links, images, quotes, rules

- `[label](relative/path)` with a repository-relative path, no leading slash, and no invented anchors.
- Images: `![alt text](relative/path)`.
- `> ` for block quotes, a blank line between quoted paragraphs.
- `---` on a blank line for a horizontal rule.
- Escape a literal markdown character with a backslash (for example a line that must start with `#` or `*`).

## Mermaid

- One diagram per fenced mermaid block (the opening fence line is a triple backtick followed by `mermaid`). House forms are `classDiagram` and `sequenceDiagram`.
- Node and member names are the real type and method names from the code. Mark new members on existing types with `(new)`.

## Common failures

- A metadata block rendering as one line: consecutive lines with no hard breaks.
- A heading, table, or fence stuck to the previous line: missing blank line.
- A line that starts with a digit and a dot accidentally becomes an ordered list.
- An unclosed code fence swallows the rest of the document.

## House conventions

- UK English and no em dashes (AGENTS.md communication style).
- Consistent terminology: the exact code names for types, members, and files.
- Cite source documents by their backticked path.
