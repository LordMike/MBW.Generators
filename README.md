# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

MBW.Generators is a small collection of Roslyn source generators for C# projects.

Each generator ships as a single NuGet package containing both the generator
and its attributes. Install the package to get the annotations and run the
generator, or reference it with `<PrivateAssets>all</PrivateAssets>` if you
only need the attributes.

## Generators

| Generator | Description |
| --- | --- |
| [NonTryMethods](src/Generators/NonTryMethods.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.NonTryMethods.svg)](https://www.nuget.org/packages/MBW.Generators.NonTryMethods) | Creates non-try wrappers for Try-pattern methods |
| [OverloadGenerator](src/Generators/OverloadGenerator.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.OverloadGenerator.svg)](https://www.nuget.org/packages/MBW.Generators.OverloadGenerator) | Generates overloads based on declarative attributes |
| [GeneratorHelpers](src/Generators/GeneratorHelpers.Generator/README.md#readme) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.GeneratorHelpers.svg)](https://www.nuget.org/packages/MBW.Generators.GeneratorHelpers) | Generates allocation-free Roslyn symbol helpers |

Alpha builds are published [to GitHub Packages](https://nuget.pkg.github.com/LordMike/index.json) - [How to use](https://blog.mbwarez.dk/posts/2021/03/consuming-my-nuget-packages-from-github/).

## Logging

All generators share a common, opt-in logger for troubleshooting. By default,
logging is disabled and generators are silent.

### Enabling logging

You can turn it on via `.editorconfig` in the root of your solution or project:

```ini
# Enable/disable (default: false)
mbw_generators_logging_enabled = true

# Optional: override the pipe name (default: MBW.Generators.Log)
mbw_generators_logging_pipeName = MBW.Generators.Log
```

Once enabled, generators will attempt to connect to a named pipe.
The default pipe name is `MBW.Generators.Log`.

### Viewing logs

Start the `Tool.LogReader` project before making edits in your IDE.
As soon as the generators connect, log messages will begin streaming in.

Typical workflow:

1. Launch `Tool.LogReader` (standalone console project).
2. Open your solution in Visual Studio / Rider / `dotnet build`.
3. Perform edits or builds; the generator will write trace messages into the log reader window.

### Notes

- Best effort only: if the logger cannot connect to the pipe, it silently disables itself for the rest of the session.
- Zero overhead when off: when `mbw_generators_logging_enabled = false`, log code paths are skipped.
- Local runs only: this logger is intended for troubleshooting and development. Do not enable it in CI builds unless you explicitly want the output.

## Status

I make open source software in my free time and share it so others can use it, learn from it, or build on it. The project is provided as-is with no guarantees about quality, correctness, or support. Issues and pull requests are welcome, but there is no guarantee of response time, fixes, or ongoing maintenance.

[Project license](LICENSE.txt).
