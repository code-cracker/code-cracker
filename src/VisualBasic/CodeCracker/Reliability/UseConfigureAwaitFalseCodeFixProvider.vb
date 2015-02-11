Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Reliability
    <ExportCodeFixProvider("CodeCrackerUseConfigureAwaitFalseCodeFixProvider", LanguageNames.VisualBasic)>
    <[Shared]>
    Public Class UseConfigureAwaitFalseCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId())
        End Function

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First()
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim awaitExpression = root.FindNode(diagnostic.Location.SourceSpan).ChildNodes.OfType(Of AwaitExpressionSyntax).First()
            Dim newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    awaitExpression.Expression.WithoutTrailingTrivia(),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName("ConfigureAwait")),
                SyntaxFactory.ArgumentList().
                    AddArguments(SyntaxFactory.SimpleArgument(SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword))
                ))).
                WithTrailingTrivia(awaitExpression.Expression.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(awaitExpression.Expression, newExpression)
            Dim newDocument = context.Document.WithSyntaxRoot(newRoot)

            context.RegisterFix(CodeAction.Create("Use ConfigureAwait(False)", newDocument),
                                diagnostic)
        End Function
    End Class
End Namespace