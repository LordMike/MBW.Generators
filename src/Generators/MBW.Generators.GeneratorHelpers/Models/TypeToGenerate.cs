using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Models;

readonly record struct TypeToGenerate(
    string ClassName,
    string Namespace,
    Accessibility Accessibility,
    FieldToGenerate[] Fields,
    List<Diagnostic> Diagnostics,
    Location TypeLocation);