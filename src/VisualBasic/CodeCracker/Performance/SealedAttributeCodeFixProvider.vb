Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Performance
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(SealedAttributeCodeFixProvider)), Composition.Shared>
    Public Class SealedAttributeCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(SealedAttributeAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diag = context.Diagnostics.First()
            Dim sourceSpan = diag.Location.SourceSpan
            Dim type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassStatementSyntax)().First()
            context.RegisterCodeFix(CodeAction.Create("Mark as NotInheritable", Function(ct) MarkClassAsSealed(context.Document, type, ct), NameOf(SealedAttributeCodeFixProvider)), diag)
        End Function

        Private Async Function MarkClassAsSealed(document As Document, type As ClassStatementSyntax, cancellationToken As Threading.CancellationToken) As Task(Of Document)
            Return document.
            WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).
            ReplaceNode(type,
                        type.WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))).
                        WithAdditionalAnnotations(Formatting.Formatter.Annotation)))

        End Function
    End Class
End Namespace