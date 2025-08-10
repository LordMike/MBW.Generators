# AGENTS

## Naming conventions
- Each source generator lives under `src/MBW.Generators.{Feature}`.
- The generator project itself is named `MBW.Generators.{Feature}`.
- Any related project (tests, abstractions, samples, etc.) should be prefixed with the same name, e.g. `MBW.Generators.{Feature}.Tests`, `MBW.Generators.{Feature}.Abstracts`.

## Building
- Build the entire solution:
  ```
  dotnet build
  ```
- Build a specific generator project:
  ```
  dotnet build src/MBW.Generators.{Feature}/MBW.Generators.{Feature}.csproj
  ```

## Testing
- Run all tests:
  ```
  dotnet test
  ```
- Run tests for a specific project:
  ```
  dotnet test src/MBW.Generators.{Feature}.Tests/MBW.Generators.{Feature}.Tests.csproj
  ```

## Documentation
- Maintain a `README.md` in the repository root containing a table summarizing all source generators and a brief description of each.
- Each generator project has its own `README.md` with detailed information and usage examples.
- Update these files whenever new generators are added or existing ones change.
