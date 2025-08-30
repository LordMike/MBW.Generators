namespace MBW.Generators.OverloadGenerator.Models;

abstract class Rule
{
    protected Rule(string parameter)
    {
        Parameter = parameter;
    }

    public string Parameter { get; }
}