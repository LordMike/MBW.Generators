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
- The repository root `README.md` introduces the project and lists all generators in a table. Each entry links to the generator's `README.md` using a relative path with the `#readme` fragment.
- Each generator project under `src/MBW.Generators.{Feature}` must have a `README.md` with these sections in order:
  1. `## About` – two or three short paragraphs describing the generator and a sample scenario.
  2. `## Quick Start` – a short bullet list covering:
     - installing both the generator and attribute packages,
     - where to apply attributes (assembly or type level),
     - the default behaviour (regexes, exceptions, generation strategy, etc.),
     - a note that attributes and generator ship in separate packages so consumers can opt out of running the generator.
  3. `## Example` – one concise example showing all attributes. Include the generated code for that example.
  4. `## Features` – bullet list summarising key capabilities of the generator.
  5. `## Attributes` – describe each attribute, where it can be applied, inheritance rules, and available options.
  6. `## More information` – state that the project is provided as-is and link to the corresponding tests project with a relative path.
- Keep examples tight: a single type with a single method.
- Update these files whenever new generators are added or existing ones change.
