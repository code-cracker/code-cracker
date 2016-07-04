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
        internal const string Title = "Replace Task.Result with await Task";
        internal const string MessageFormat = "await '{0}' rather than calling its Result.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "Calling Task.Result in an awaited method may lead to a deadlock. "
            + "Obtain the result of the task with the await keyword to avoid deadlocks. ";

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