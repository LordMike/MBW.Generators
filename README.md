# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

Various source code generators I have created

### NonTryMethods [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.svg)](https://www.nuget.org/packages/MBW.Generators) [![GHPackages](https://img.shields.io/badge/package-alpha-green)](https://github.com/LordMike/MBW.Generators/packages/748396)

This generator automatically creates new extension methods that repliace existing Try-methods, with non-Try variants.

* New classes maintain the same modifiers and namespace as existing classes
* New non-try methods maintain the same modifiers as existing methods
* Only method patterns that qualify are wrapped:
  * Must be `static` (ofc.)
  * Must return a `bool`
  * Must have 0..1 `out` parameter, which must be the last parameter

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

