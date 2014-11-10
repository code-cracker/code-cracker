using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.EmptyObjectInitializerAnalyzer";
        internal const string Title = "Empty Object Initializer";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ObjectCreationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = context.Node as ObjectCreationExpressionSyntax;

            if (objectCreation.Initializer != null && !objectCreation.Initializer.Expressions.Any())
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.Initializer.OpenBraceToken.GetLocation(), "Remove empty object initializer.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
