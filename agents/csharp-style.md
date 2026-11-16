# C# style

House rules for C# in this repository. Applies to all `*.cs` under `src/` and `tests/`.

## Member ordering

Member order is enforced at build time by the StyleCop analyzer rules `SA1201`, `SA1202`, `SA1214` and `SA1215`, configured in `.editorconfig` through `stylecop.ordering.memberKindOrder` and the rule severities. This document states the house order that configuration enforces.

Elements within a type appear in this order:

1. Fields
2. Constructors, static before instance. A finalizer, if any, follows the last constructor
3. Delegate declarations
4. Properties, including indexers
5. Events
6. Methods
7. Operators
8. Nested types, last. Structs before classes. Nested enums and interfaces are unconstrained

Within each group, elements are ordered by access: `public`, `internal`, `protected internal`, `protected`, `private`. Within the same access level, fields are ordered `const`, `static readonly`, `static`, `readonly`, mutable, and static methods come before instance methods.

Consecutive members are separated by a single blank line.

Top-level declarations at file scope follow the StyleCop default order, which `SA1201` also enforces.

When adding a member, insert it into its group at the position its access level dictates, not at the end of the class.

## Local variable declarations

Prefer `var` over an explicit type when the static type is apparent from the initializer: `new` with an explicit type name, a cast, a built-in literal, or a member access whose return type is self-evident. This matches the IDE analyzer settings in `.editorconfig` (`csharp_style_var_for_built_in_types`, `csharp_style_var_when_type_is_apparent`, `csharp_style_var_elsewhere`).

Declare the type explicitly when the initializer does not make the type apparent: a value assigned to a narrower interface or base type without a cast (for example a substitute or factory implementing several interfaces), or where the explicit type documents a conversion the reader must see.
