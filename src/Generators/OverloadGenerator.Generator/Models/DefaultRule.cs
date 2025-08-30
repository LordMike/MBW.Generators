namespace MBW.Generators.OverloadGenerator.Generator.Models;

sealed class DefaultRule : Rule
{
    public DefaultRule(string parameter, string expression)
        : base(parameter)
    {
        Expression = expression;
    }

    public string Expression { get; }
}