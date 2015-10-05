Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Design
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(NameOfCodeFixProvider)), Composition.Shared>
    Public Class NameOfCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(NameOfAnalyzer.Id, DiagnosticId.NameOf_External.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim diagnosticspan = diagnostic.Location.SourceSpan
            Dim stringLiteral = root.FindToken(diagnosticspan.Start).Parent.AncestorsAndSelf.OfType(Of LiteralExpressionSyntax).FirstOrDefault
            If stringLiteral IsNot Nothing Then
                context.RegisterCodeFix(CodeAction.Create("use NameOf()", Function(c) MakeNameOf(context.Document, stringLiteral, root), NameOf(NameOfCodeFixProvider)), diagnostic)
            End If
        End Function

        Private Function MakeNameOf(document As Document, stringLiteral As LiteralExpressionSyntax, root As SyntaxNode) As Task(Of Document)
            Dim newNameof = SyntaxFactory.ParseExpression($"NameOf({stringLiteral.Token.ToString().Replace("""", "")})").
                WithLeadingTrivia(stringLiteral.GetLeadingTrivia).
                WithTrailingTrivia(stringLiteral.GetTrailingTrivia).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(stringLiteral, newNameof)
            Return Task.FromResult(document.WithSyntaxRoot(newRoot))
        End Function

    End Class
End Namespace