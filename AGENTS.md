# AGENTS

## Naming conventions
- Each feature lives under `src/Generators`.
- Projects use the prefix `MBW.Generators.{Feature}`:
  - `MBW.Generators.{Feature}` – meta project for NuGet packing.
  - `MBW.Generators.{Feature}.Generator` – source generator.
  - `MBW.Generators.{Feature}.Attributes` – attribute definitions.
  - `MBW.Generators.{Feature}.Tests` – tests.
- Any additional project (abstractions, samples, etc.) should keep the same prefix.

## Project structure
- Generators, attributes, tests, and a meta packing project are wired together via the `_Imports` targets.
- During development the generator references the attributes project so editors can navigate attribute sources and namespaces.
- The meta project uses `_Imports/Package.targets` and `_Imports/SourceGenerator.targets` to embed generator and attribute sources into a single NuGet package while avoiding additional package dependencies.

## Building
- Build the entire solution:
  ```
  dotnet build
  ```
  - Build a specific generator project:
    ```
    dotnet build src/Generators/{Feature}.Generator/{Feature}.Generator.csproj
    ```

## Testing
- Run all tests:
  ```
  dotnet test
  ```
  - Run tests for a specific project:
    ```
    dotnet test src/Generators/{Feature}.Tests/{Feature}.Tests.csproj
    ```

When making new tests, ensure we test likely scenarios a user might encounter or use the Generators in. Avoid testing invalid code, that is, if something wouldn't be syntactically valid code, the user will fix that first before the generator can be expected to produce a valid output.

## Documentation
- The repository root `README.md` introduces the project and lists all generators in a table. Each entry links to the generator's `README.md` using a relative path with the `#readme` fragment.
  - Each generator project under `src/Generators/{Feature}.Generator` must have a `README.md` with these sections in order:
  1. `## About` – two or three short paragraphs describing the generator and a sample scenario.
  2. `## Quick Start` – a short bullet list covering:
      - installing the generator package (attributes included),
     - where to apply attributes (assembly or type level),
     - the default behaviour (regexes, exceptions, generation strategy, etc.),
      - a note that the package includes both the attributes and the generator.
  3. `## Example` – one concise example showing all attributes. Include the generated code for that example.
  4. `## Features` – bullet list summarising key capabilities of the generator.
  5. `## Attributes` – describe each attribute, where it can be applied, inheritance rules, and available options.
  6. `## More information` – state that the project is provided as-is and link to the corresponding tests project with a relative path.
- Keep examples tight: a single type with a single method.
- Update these files whenever new generators are added or existing ones change.
