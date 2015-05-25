Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(MustInheritClassShouldNotHavePublicConstructorsCodeFixProvider)), Composition.Shared>
    Public Class MustInheritClassShouldNotHavePublicConstructorsCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diag = context.Diagnostics.First()
            Dim span = diag.Location.SourceSpan

            Dim constructor = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of SubNewStatementSyntax)
            context.RegisterCodeFix(CodeAction.Create("Use 'Protected' in stead of 'Public'", Function(c) ReplacePublicWithProtectedAsync(context.Document, constructor, c)), diag)
        End Function


        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId())

        Private Async Function ReplacePublicWithProtectedAsync(document As Document, constructor As SubNewStatementSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim [public] = constructor.Modifiers.First(Function(m) m.IsKind(SyntaxKind.PublicKeyword))
            Dim [protected] = SyntaxFactory.Token([public].LeadingTrivia, SyntaxKind.ProtectedKeyword, [public].TrailingTrivia)

            Dim newModifiers = constructor.Modifiers.Replace([public], [protected])
            Dim newConstructor = constructor.WithModifiers(newModifiers)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = root.ReplaceNode(constructor, newConstructor)
            Dim newDocumnent = document.WithSyntaxRoot(newRoot)
            Return newDocumnent
        End Function
    End Class
End Namespace