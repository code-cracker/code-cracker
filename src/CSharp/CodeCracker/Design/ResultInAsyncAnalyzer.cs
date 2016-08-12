using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ResultInAsyncAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ResultInAsyncAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ResultInAsync_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ResultInAsync_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Design;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ResultInAsync.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ResultInAsync));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocation = (InvocationExpressionSyntax)context.Node;
            var parentMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod == null) return;
            var parentIsAsync = parentMethod.Modifiers.Any(n => n.IsKind(SyntaxKind.AsyncKeyword));
            if (!parentIsAsync) return;
            // We now know that we are in async method

            var memberAccess = invocation.Parent as MemberAccessExpressionSyntax;
            if (memberAccess == null) return;
            var member = memberAccess.Name;
            if (member.ToString() != "Result") return;
            // We now know that we are accessing .Result

            var identifierSymbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
            if (identifierSymbol.OriginalDefinition.ToString() != "System.Threading.Tasks.Task<TResult>.Result") return;
            // We now know that we are accessing System.Threading.Tasks.Task<TResult>.Result

            SimpleNameSyntax identifier;
            identifier = invocation.Expression as IdentifierNameSyntax;
            if (identifier == null)
            {
                var transient = invocation.Expression as MemberAccessExpressionSyntax;
                identifier = transient.Name;
            }
            if (identifier == null) return; // It's not supposed to happen. Don't throw an exception, though.
            context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), identifier.Identifier.Text));
        }
    }
}