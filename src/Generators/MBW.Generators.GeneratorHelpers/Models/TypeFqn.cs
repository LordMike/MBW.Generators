namespace MBW.Generators.GeneratorHelpers.Models;

readonly record struct TypeFqn(string[] NamespaceSegments, TypeSegment[] TypeSegments, string Normalized);