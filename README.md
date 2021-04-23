# MBW.Generators [![Generic Build](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Generators/actions/workflows/dotnet.yml)

Various source code generators I have created

### NonTryMethods [![NuGet](https://img.shields.io/nuget/v/MBW.Generators.svg)](https://www.nuget.org/packages/MBW.Generators) [![GHPackages](https://img.shields.io/badge/package-alpha-green)](https://github.com/LordMike/MBW.Generators/packages/)

This generator automatically creates new extension methods that repliace existing Try-methods, with non-Try variants.

#### Install 

Reference the nuget package, the rest should come that way around

#### Example

```csharp
// Existing code
static class ExistingExtensions 
{
	bool TryNNN(this MyClass instance, out object someProperty)
}

// Auto-generated code
static class ExistingExtensions_AutogenNonTry
{
	object NNN(this MyClass instance)
	{
		if (!instance.TryNNN(out object result))
			throw new Exception();

		return result;
	}
}
```

