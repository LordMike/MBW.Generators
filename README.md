# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

A small collection of Roslyn source generators.

Each generator ships as two NuGet packages: the generator itself and a companion
`*.Attributes` package containing only the attributes. Reference both packages
to run the generator, or reference just the attributes package when you merely
want the annotations available without executing the generator.

## Generators

| Generator | Description |
|-----------|-------------|
| [NonTryMethods](src/MBW.Generators.NonTryMethods/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.NonTryMethods.svg)](https://www.nuget.org/packages/MBW.Generators.NonTryMethods) | Creates non-try wrappers for Try-pattern methods |
| [OverloadGenerator](src/MBW.Generators.OverloadGenerator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.OverloadGenerator.svg)](https://www.nuget.org/packages/MBW.Generators.OverloadGenerator) | Generates overloads based on declarative attributes |
