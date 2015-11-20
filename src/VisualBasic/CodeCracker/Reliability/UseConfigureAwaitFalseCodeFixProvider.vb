Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Reliability
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(UseConfigureAwaitFalseCodeFixProvider)), Composition.Shared>
    Public Class UseConfigureAwaitFalseCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId())

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create("Use ConfigureAwait(False)", Function(c) CreateUseConfigureAwaitAsync(context.Document, diagnostic, c), NameOf(UseConfigureAwaitFalseCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function
        Private Shared Async Function CreateUseConfigureAwaitAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim awaitExpression = root.FindNode(diagnostic.Location.SourceSpan).ChildNodes.OfType(Of AwaitExpressionSyntax).FirstOrDefault()
            If awaitExpression Is Nothing Then Return document

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
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class
End Namespace