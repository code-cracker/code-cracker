Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Performance
    <ExportCodeFixProvider("CodeCrackerMakeLocalVariableConstWhenItIsPossibleCodeFixProvider", LanguageNames.VisualBasic), Composition.Shared>
    Public Class MakeLocalVariableConstWhenPossibleCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(MakeLocalVariableConstWhenPossibleAnalyzer.Id)
        End Function

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            Dim localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType(Of LocalDeclarationStatementSyntax).First()
            Const message = "Make constant"
            context.RegisterFix(CodeAction.Create(message, Function(c) MakeConstantAsync(context.Document, localDeclaration, c)), diagnostic)

        End Function

        Public Async Function MakeConstantAsync(document As Document, localDeclaration As LocalDeclarationStatementSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim declaration = localDeclaration.Declarators.First

            Dim dimModifier = localDeclaration.Modifiers.First()

            Dim constant = SyntaxFactory.Token(SyntaxKind.ConstKeyword).
            WithLeadingTrivia(dimModifier.LeadingTrivia).
            WithTrailingTrivia(dimModifier.TrailingTrivia).
            WithAdditionalAnnotations(Formatter.Annotation)

            Dim modifiers = localDeclaration.Modifiers.Replace(dimModifier, constant)

            Dim newLocalDeclaration = localDeclaration.
            WithModifiers(modifiers).
            WithLeadingTrivia(localDeclaration.GetLeadingTrivia()).
            WithTrailingTrivia(localDeclaration.GetTrailingTrivia()).
            WithAdditionalAnnotations(Formatter.Annotation)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim newRoot = root.ReplaceNode(localDeclaration, newLocalDeclaration)
            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace