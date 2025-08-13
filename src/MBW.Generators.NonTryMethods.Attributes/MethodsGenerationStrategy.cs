namespace MBW.Generators.NonTryMethods.Attributes;

public enum MethodsGenerationStrategy
{
    /// <summary>
    /// Automatically picks a strategy. Methods will be generated in partial types
    /// </summary>
    Auto = 0,
    
    /// <summary>
    /// Generated non-try methods should be in partial types. Requires the relevant types to be marked partial as well
    /// </summary>
    PartialType = 1,
    
    /// <summary>
    /// Generated non-try-methods should be in extensions types. 
    /// </summary>
    Extensions = 2
}