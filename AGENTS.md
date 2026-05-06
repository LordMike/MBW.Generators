# AGENTS

## Purpose
MBW.Generators is a .NET solution for Roslyn source generator packages.
Optimize for minimal generator output changes, stable NuGet packaging, and examples that match real user scenarios.

## Do / Don't
- Do: keep generator, attributes, tests, and meta packing projects wired through the existing `_Imports` targets.
- Do: update root and generator README files when generator behavior, attributes, or package listings change.
- Do: add tests for likely valid-code scenarios users can encounter.
- Don't: test syntactically invalid code as a generator responsibility.
- Don't: introduce package dependencies into the packed generator packages unless the packing strategy is explicitly revisited.

## Pushback / quality bar
- Before starting new projects or automation, evaluate whether the effort is justified and push back if build time exceeds the time it would save.
- If a request would introduce hacks, unclear behavior, or long-term maintenance risk, push back and propose a safer alternative.
- Avoid obvious performance pitfalls; call them out and offer a better approach.
- Prefer clear, simple code over clever or verbose implementations.

## Core workflows
- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/Tools/Tool.LogReader/Tool.LogReader.csproj` only when troubleshooting generator logging; DO NOT RUN AUTOMATICALLY UNLESS ASKED TO.
- Build one generator project: `dotnet build src/Generators/{Feature}.Generator/{Feature}.Generator.csproj`
- Test one generator: `dotnet test src/Generators/{Feature}.Tests/{Feature}.Tests.csproj`

## Repo conventions
- Each feature lives under `src/Generators`.
- Projects use the prefix `MBW.Generators.{Feature}`:
  - `MBW.Generators.{Feature}` - meta project for NuGet packing.
  - `MBW.Generators.{Feature}.Generator` - source generator.
  - `MBW.Generators.{Feature}.Attributes` - attribute definitions.
  - `MBW.Generators.{Feature}.Tests` - tests.
- Any additional project (abstractions, samples, etc.) should keep the same prefix.
- Generators, attributes, tests, and a meta packing project are wired together via the `_Imports` targets.
- During development the generator references the attributes project so editors can navigate attribute sources and namespaces.
- The meta project uses `_Imports/Package.targets` and `_Imports/SourceGenerator.targets` to embed generator and attribute sources into a single NuGet package while avoiding additional package dependencies.
- C# style: standard PascalCase for public members, camelCase for locals and fields, 4-space indentation, and braces on a new line.

## Documentation upkeep
- `README.md` - introduces the project and lists all generators in a table. Each entry links to the generator's `README.md` using a relative path with the `#readme` fragment.
- `src/Generators/{Feature}.Generator/README.md` - update whenever a generator's behavior, attributes, package name, or examples change. Use these sections in order:
  1. `## About` - two or three short paragraphs describing the generator and a sample scenario.
  2. `## Quick Start` - a short bullet list covering:
     - installing the generator package (attributes included),
     - where to apply attributes (assembly or type level),
     - the default behavior (regexes, exceptions, generation strategy, etc.),
     - a note that the package includes both the attributes and the generator.
  3. `## Example` - one concise example showing all attributes. Include the generated code for that example.
  4. `## Features` - bullet list summarizing key capabilities of the generator.
  5. `## Attributes` - describe each attribute, where it can be applied, inheritance rules, and available options.
  6. `## More information` - state that the project is provided as-is and link to the corresponding tests project with a relative path.
- Keep examples tight: a single type with a single method.
- Update these files whenever new generators are added or existing ones change.

## When to split
If this file grows beyond a page, or if the repo has distinct task areas, ask the user whether to split into sub-AGENTS files and route by task. Sub-agents must be named `AGENTS.<TASK>.md`. When sub-agents exist, the main `AGENTS.md` should be mostly a redirector and general guidance.
