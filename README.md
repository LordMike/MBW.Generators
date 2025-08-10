# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

Various source code generators I have created

## Generators

| Generator | Description |
|-----------|-------------|
| [NonTryMethods](src/MBW.Generators.NonTryMethods/README.md) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.NonTryMethods.svg)](https://www.nuget.org/packages/MBW.Generators.NonTryMethods) | Creates non-try wrappers for Try-pattern methods |
| [OverloadGenerator](src/MBW.Generators.OverloadGenerator/README.md) [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.OverloadGenerator.svg)](https://www.nuget.org/packages/MBW.Generators.OverloadGenerator) | Generates overloads based on declarative attributes |

### NonTryMethods [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.NonTryMethods.svg)](https://www.nuget.org/packages/MBW.Generators.NonTryMethods) [![GHPackages](https://img.shields.io/badge/package-alpha-green)](https://github.com/LordMike/MBW.Generators/packages/748396)

This generator automatically creates new extension methods that repliace existing Try-methods, with non-Try variants.

* New classes maintain the same modifiers and namespace as existing classes
* New non-try methods maintain the same modifiers as existing methods
* Attribute classes with `GenerateNonTryMethod` to enable wrapping for them - this attribute is not compiled into the code
* Only method patterns that qualify are wrapped:
  * Must be `static` (ofcourse)
  * Must return a `bool`
  * Must have 0..1 `out` parameters, which must be the last parameter

#### Install

Reference the nuget package, the rest should come that way around

#### Example

```csharp
// Existing code
public static class ExistingExtensions
{
        public static bool TryNNN(this MyClass instance, out object someProperty)
}

// Auto-generated code
public static class ExistingExtensions_AutogenNonTry
{
        public static object NNN(this MyClass instance)
        {
                if (!instance.TryNNN(out object result))
                        throw new Exception();

                return result;
        }
}
```
