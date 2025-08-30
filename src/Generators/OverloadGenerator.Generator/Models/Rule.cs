namespace MBW.Generators.OverloadGenerator.Generator.Models;

abstract class Rule
{
    protected Rule(string parameter)
    {
        Parameter = parameter;
    }

    public string Parameter { get; }
}