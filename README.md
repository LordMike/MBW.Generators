# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

A small collection of Roslyn source generators.

Each generator ships as a single NuGet package containing both the generator
and its attributes. Install the package to get the annotations and run the
generator, or reference it with `<PrivateAssets>all</PrivateAssets>` if you
only need the attributes.

## Generators

| Generator | Description |
|-----------|-------------|
| [NonTryMethods](src/Generators/NonTryMethods.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.NonTryMethods.svg)](https://www.nuget.org/packages/MBW.Generators.NonTryMethods) | Creates non-try wrappers for Try-pattern methods |
| [OverloadGenerator](src/Generators/OverloadGenerator.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.OverloadGenerator.svg)](https://www.nuget.org/packages/MBW.Generators.OverloadGenerator) | Generates overloads based on declarative attributes |
| [GeneratorHelpers](src/Generators/GeneratorHelpers.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.GeneratorHelpers.svg)](https://www.nuget.org/packages/MBW.Generators.GeneratorHelpers) | Generates allocation-free Roslyn symbol helpers |
