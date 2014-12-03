using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker
{
    [ExportCodeFixProvider("ConvertSimpleLambdaExpressionToMethodInvocationFixProvider", LanguageNames.CSharp), Shared]
    public class ConvertSimpleLambdaExpressionToMethodInvocationFixProvider
        :CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ConvertSimpleLambdaExpressionToMethodInvocationAnalizer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var lambda = root.FindToken(diagnosticSpan.Start).Parent
                .AncestorsAndSelf().OfType<SimpleLambdaExpressionSyntax>().First();

            var methodInvoke = lambda.Body as InvocationExpressionSyntax;
            var methodName = methodInvoke.Expression as IdentifierNameSyntax;

            root = root.ReplaceNode(lambda as ExpressionSyntax, methodName as ExpressionSyntax);
            var newDocument = context.Document.WithSyntaxRoot(root);

            // verify that the conversion is correct
            var newSemanticModel = await newDocument.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var identifierToken = newRoot.FindToken(diagnosticSpan.Start);
            var newDiagnostics = newSemanticModel.Compilation.GetDiagnostics(context.CancellationToken);
            var diagnosticErrors = newDiagnostics.WhereAsArray(i => i.Severity == DiagnosticSeverity.Error);
            if (diagnosticErrors.Any(error => LocationOverlapsIdentifierName(identifierToken.Parent, error)))
                return;

            context.RegisterFix(CodeAction.Create(
                "Use method name instead of lambda expression when signatures match",
                newDocument), diagnostic);
        }

        private bool LocationOverlapsIdentifierName(SyntaxNode node, Diagnostic error)
        {
            var errorLocation = error.Location;
            if (errorLocation == null)
                return false;

            var nodeLocation = node.GetLocation();
            if (nodeLocation == null)
                return false;

            if (errorLocation.SourceTree != nodeLocation.SourceTree)
                return false;

            return nodeLocation.SourceSpan.OverlapsWith(errorLocation.SourceSpan);
        }
    }
}
