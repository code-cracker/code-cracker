Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ArgumentExceptionCodeFixProvider)), Composition.Shared>
    Public Class ArgumentExceptionCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.ArgumentException.ToDiagnosticId())

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First()
            Dim span = diagnostic.Location.SourceSpan
            Dim objectCreation = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of ObjectCreationExpressionSyntax)

            Dim parameters = ArgumentExceptionAnalyzer.GetParameterNamesFromCreationContext(objectCreation)
            For Each param In parameters
                Dim message = "Use '" & param & "'"
                context.RegisterCodeFix(CodeAction.Create(message, Function(c) FixParamAsync(context.Document, objectCreation, param, c)), diagnostic)
            Next
        End Function

        Private Async Function FixParamAsync(document As Document, objectCreation As ObjectCreationExpressionSyntax, newParamName As String, cancellationToken As CancellationToken) As Task(Of Document)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)

            Dim argumentList = objectCreation.ArgumentList
            Dim paramNameLiteral = DirectCast(argumentList.Arguments(1).GetExpression, LiteralExpressionSyntax)
            Dim paramNameOpt = semanticModel.GetConstantValue(paramNameLiteral)
            Dim currentParamName = paramNameOpt.Value.ToString()

            Dim newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newParamName))
            Dim root = Await document.GetSyntaxRootAsync()
            Dim newRoot = root.ReplaceNode(paramNameLiteral, newLiteral)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function

    End Class
End Namespace