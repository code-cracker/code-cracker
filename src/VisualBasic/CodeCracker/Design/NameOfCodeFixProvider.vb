Imports CodeCracker.Properties
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

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create(Resources.NameOfAnalyzer_Title, Function(c) MakeNameOf(context.Document, diagnostic, c), NameOf(NameOfCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function MakeNameOf(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim diagnosticspan = diagnostic.Location.SourceSpan
            Dim stringLiteral = root.FindToken(diagnosticspan.Start).Parent.AncestorsAndSelf.OfType(Of LiteralExpressionSyntax).FirstOrDefault

            Dim newNameof = SyntaxFactory.ParseExpression($"NameOf({stringLiteral.Token.ToString().Replace("""", "")})").
                WithLeadingTrivia(stringLiteral.GetLeadingTrivia).
                WithTrailingTrivia(stringLiteral.GetTrailingTrivia).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(stringLiteral, newNameof)
            Return document.WithSyntaxRoot(newRoot)
        End Function

    End Class
End Namespace