using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameOfAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0021";
        internal const string Title = "You should use 'nameof({0})' whenever possible.";
        internal const string MessageFormat = "Use 'nameof({0})' instead of specifying the parameter name.";
        internal const string Category = "Syntax";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.StringLiteralExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var stringLiteral = context.Node as LiteralExpressionSyntax;
            if (string.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText)) return;

            var methodDeclaration = stringLiteral.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration == null) return;

            var methodParameters = methodDeclaration.ParameterList.Parameters;
            if (!methodParameters.Any(m => m.Identifier.Value.ToString() == stringLiteral.Token.Value.ToString())) return;

            var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.Value);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
