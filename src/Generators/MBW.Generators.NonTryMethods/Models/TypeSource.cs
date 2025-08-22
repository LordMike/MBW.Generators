using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods.Models;

internal record struct TypeSource(string HintName, SourceText Source);