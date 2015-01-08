Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeFixProvider("CodeCrackerNameOfCodeFixProvider", LanguageNames.VisualBasic)>
Public Class NameOfCodeFixProvider
    Inherits CodeFixProvider

    Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
        Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
        Dim diagnostic = context.Diagnostics.First
        Dim diagnosticspan = diagnostic.Location.SourceSpan
        Dim stringLiteral = root.FindToken(diagnosticspan.Start).Parent.AncestorsAndSelf.OfType(Of LiteralExpressionSyntax).FirstOrDefault
        If stringLiteral IsNot Nothing Then
            context.RegisterFix(CodeAction.Create("use nameof()", Function(c) MakeNameOfAsync(context.Document, stringLiteral, c)), diagnostic)
        End If
    End Function

    Private Async Function MakeNameOfAsync(document As Document, stringLiteral As LiteralExpressionSyntax, cancellationToken As CancellationToken) As Task(Of Document)
        Dim methodDeclaration = stringLiteral.AncestorsAndSelf().OfType(Of MethodBlockSyntax).FirstOrDefault
        If methodDeclaration IsNot Nothing Then
            Dim methodParam = methodDeclaration.Begin.ParameterList.Parameters.First
            Return Await NewDocument(document, stringLiteral, methodParam)
        Else
            Dim constructorDeclaration = stringLiteral.AncestorsAndSelf.OfType(Of ConstructorBlockSyntax).FirstOrDefault
            Dim constructorParam = constructorDeclaration.Begin.ParameterList.Parameters.First

            Return Await NewDocument(document, stringLiteral, constructorParam)
        End If
    End Function

    Private Async Function NewDocument(document As Document, stringLiteral As LiteralExpressionSyntax, methodParameter As ParameterSyntax) As Task(Of Document)
        Dim newNameof = SyntaxFactory.ParseExpression(String.Format("nameof({0})", methodParameter.Identifier.Identifier.ValueText)).
            WithLeadingTrivia(stringLiteral.GetLeadingTrivia).
            WithTrailingTrivia(stringLiteral.GetTrailingTrivia).
            WithAdditionalAnnotations(Formatter.Annotation)

        Dim root = Await document.GetSyntaxRootAsync()
        Dim newRoot = root.ReplaceNode(stringLiteral, newNameof)
        Return document.WithSyntaxRoot(newRoot)
    End Function

    Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
        Return ImmutableArray.Create(NameOfAnalyzer.DiagnosticId)
    End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
        Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
