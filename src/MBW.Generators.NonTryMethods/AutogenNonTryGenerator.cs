using System.Linq;
using System.Text;
using MBW.Generators.NonTryMethods.Objects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods
{
    [Generator]
    public class AutogenNonTryGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();

            context.RegisterForSyntaxNotifications(() => new AutogenNonTryGeneratorSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            AutogenNonTryGeneratorSyntaxReceiver receiver = (AutogenNonTryGeneratorSyntaxReceiver)context.SyntaxReceiver;

            foreach (EligibleClassContainer @class in receiver.Classes)
            {
                string newClassName = $"{@class.Class.Identifier.ValueText}_AutogenNonTry";

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("using System;");
                foreach (UsingDirectiveSyntax usingDirectiveSyntax in @class.Usings)
                    sb.Append(usingDirectiveSyntax.ToFullString());

                sb.Append("namespace ").AppendLine(@class.Namespace.Name.ToString());
                sb.AppendLine("{");
                sb.Append("    public static class ")
                    .AppendLine(newClassName);
                sb.AppendLine("    {");

                foreach (MethodDeclarationSyntax method in @class.Methods)
                {
                    SeparatedSyntaxList<ParameterSyntax> @params = method.ParameterList.Parameters;

                    ParameterSyntax thisParam = @params.First();
                    bool hasSingleOutParam = @params.Take(@params.Count - 1).All(x => !x.Modifiers.Any(SyntaxKind.OutKeyword)) &&
                                             @params.Last().Modifiers.Any(SyntaxKind.OutKeyword);

                    ParameterSyntax[] arguments;
                    if (hasSingleOutParam)
                        arguments = @params.Skip(1).Take(@params.Count - 2).ToArray();
                    else
                        arguments = @params.Skip(1).ToArray();

                    string argumentsCommaStr = arguments.Any() ? ", " : "";
                    string argumentsStr = string.Join(", ", arguments.Select(s => s.ToFullString()));
                    string argumentsNamesStr = string.Join(", ", arguments.Select(s => s.Identifier.Text));

                    string outType;
                    if (hasSingleOutParam)
                        outType = @params.Last().Type.ToString();
                    else
                        outType = "void";

                    sb.Append("        public static ")
                        .Append(outType)
                        .Append(" ")
                        .Append(method.Identifier.Text.Substring("Try".Length))
                        .Append("(").Append(thisParam.ToFullString()).Append(argumentsCommaStr).Append(argumentsStr).AppendLine(")");

                    sb.AppendLine("        {");
                    sb.AppendLine(
                        $"            if (!{thisParam.Identifier.Text}.{method.Identifier.Text}({argumentsNamesStr}{argumentsCommaStr}out {outType} result))");
                    sb.AppendLine("                throw new Exception();");
                    sb.AppendLine("");
                    sb.AppendLine("            return result;");
                    sb.AppendLine("        }");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                // inject the created source into the users compilation
                context.AddSource(newClassName, SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

    }
}