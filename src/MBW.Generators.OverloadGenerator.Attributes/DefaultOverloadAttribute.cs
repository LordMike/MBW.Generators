using System;

namespace MBW.Generators.OverloadGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class DefaultOverloadAttribute : Attribute
    {
        public DefaultOverloadAttribute(string parameter, string defaultExpression)
        {
            Parameter = parameter;
            DefaultExpression = defaultExpression;
        }

        public string Parameter { get; }
        public string DefaultExpression { get; }
        public string[]? Usings { get; set; }
    }
}
