using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class JsonNetAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your Json syntax is wrong";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "This diagnostic checks the json string and triggers if the parsing fail "
            + "by throwing an exception.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.JsonNet.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.JsonNet));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(c => Analyzer(c, "DeserializeObject", "Newtonsoft.Json.JsonConvert.DeserializeObject<T>(string)"), SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(c => Analyzer(c, "Parse", "Newtonsoft.Json.Linq.JObject.Parse(string)"), SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(c => Analyzer(c, "Parse", "Newtonsoft.Json.Linq.JArray.Parse(string)"), SyntaxKind.InvocationExpression);
        }

        private static void Analyzer(SyntaxNodeAnalysisContext context, string methodName, string methodFullDefinition)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpression?.Name?.Identifier.ValueText != methodName) return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpression).Symbol;
            if (memberSymbol?.OriginalDefinition?.ToString() != methodFullDefinition) return;

            var argumentList = invocationExpression.ArgumentList;
            if ((argumentList?.Arguments.Count ?? 0) != 1) return;

            var literalParameter = argumentList.Arguments[0].Expression as LiteralExpressionSyntax;
            if (literalParameter == null) return;

            var jsonOpt = context.SemanticModel.GetConstantValue(literalParameter);
            var json = jsonOpt.Value as string;

            CheckJsonValue(context, literalParameter, json);
        }

        private static void CheckJsonValue(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literalParameter,
            string json)
        {
            try
            {
                parseMethodInfo.Value.Invoke(null, new[] { json });
            }
            catch (Exception ex)
            {
                var diag = Diagnostic.Create(Rule, literalParameter.GetLocation(), ex.InnerException.Message);
                context.ReportDiagnostic(diag);
            }
        }

        private static readonly Lazy<Type> jObjectType = new Lazy<Type>(() => Type.GetType("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json"));
        private static readonly Lazy<MethodInfo> parseMethodInfo = new Lazy<MethodInfo>(() => jObjectType.Value.GetRuntimeMethod("Parse", new[] { typeof(string) }));
    }
}