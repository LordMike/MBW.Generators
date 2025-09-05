using System.Text.RegularExpressions;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal sealed record GenerateNonTryMethodAttributeInfoWithValidPattern(
    string ExceptionTypeName,
    string MethodNamePattern,
    Regex Pattern);