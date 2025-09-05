using MBW.Generators.GeneratorHelpers;
using MBW.Generators.GeneratorHelpers.Attributes;

namespace MBW.Generators.NonTryMethods.Generator.Models;

[GenerateSymbolExtensions]
internal static partial class KnownSymbols
{
    [SymbolNameExtension]
    public const string GenerateNonTryMethodAttribute =
        "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryMethodAttribute";

    [SymbolNameExtension]
    public const string GenerateNonTryOptionsAttribute =
        "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryOptionsAttribute";

    [SymbolNameExtension]
    public const string TaskOfT = "System.Threading.Tasks.Task`1";

    [SymbolNameExtension]
    public const string ValueTaskOfT = "System.Threading.Tasks.ValueTask`1";

    public const string ExceptionBase = "System.Exception";

    public const string InvalidOperationException = "System.InvalidOperationException";
}

