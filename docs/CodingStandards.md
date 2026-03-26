# Coding standards

## Language policy
- Use English-only comments in source code.
- Use English-only UI strings and labels. Localized variants can be added later, but the default resources must stay in English.

## Nullability and analyzers
- Nullable reference types are enabled at solution level through `Directory.Build.props`.
- .NET analyzers are enabled and code-style diagnostics are enforced during build.
- New projects should inherit solution-level settings and avoid per-project overrides unless strictly required.

## Test expectations
- Unit tests should cover command builders, operation validators, queue transitions, and probe parsing logic.
- Integration tests should use deterministic fixtures and produce deterministic assertions.
