using System;
using System.Collections.Immutable;
using System.Reflection;
using CodeCracker.CSharp.Usage.MethodAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IPAddressAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your IP Address syntax is wrong.";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Usage;

        private const string Description =
            "This diagnostic checks the IP Address string and triggers if the parsing fail "
            + "by throwing an exception.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.IPAddress.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.IPAddress));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var method = new MethodInformation(
                "Parse",
                "System.Net.IPAddress.Parse(string)",
                args =>
                {
                    parseMethodInfo.Value.Invoke(null, new[] { args[0].ToString() });
                }
                );
            var checker = new MethodChecker(context, Rule);
            checker.AnalyzeMethod(method);
        }

        private static readonly Lazy<Type> objectType = new Lazy<Type>(() => Type.GetType("System.Net.IPAddress, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));

        private static readonly Lazy<MethodInfo> parseMethodInfo =
            new Lazy<MethodInfo>(() => objectType.Value.GetRuntimeMethod("Parse", new[] { typeof(string) }));
    }
}