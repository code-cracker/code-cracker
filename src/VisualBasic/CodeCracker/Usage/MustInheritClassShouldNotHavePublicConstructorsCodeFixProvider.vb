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

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diag = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create("Use 'Friend' instead of 'Public'", Function(c) ReplacePublicWithProtectedAsync(context.Document, diag, c), NameOf(MustInheritClassShouldNotHavePublicConstructorsCodeFixProvider)), diag)
            Return Task.FromResult(0)
        End Function


        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId())

        Private Async Function ReplacePublicWithProtectedAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim span = diagnostic.Location.SourceSpan

            Dim constructor = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of SubNewStatementSyntax)

            Dim [public] = constructor.Modifiers.First(Function(m) m.IsKind(SyntaxKind.PublicKeyword))
            Dim [protected] = SyntaxFactory.Token([public].LeadingTrivia, SyntaxKind.ProtectedKeyword, [public].TrailingTrivia)

            Dim newModifiers = constructor.Modifiers.Replace([public], [protected])
            Dim newConstructor = constructor.WithModifiers(newModifiers)
            Dim newRoot = root.ReplaceNode(constructor, newConstructor)
            Dim newDocumnent = document.WithSyntaxRoot(newRoot)
            Return newDocumnent
        End Function
    End Class
End Namespace