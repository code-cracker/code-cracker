Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic

<ExportCodeFixProvider("CodeCrackerSealedAttributeCodeFixProvider", LanguageNames.VisualBasic)>
Public Class SealedAttributeCodeFixProvider
    Inherits CodeFixProvider

    Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
        Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
        Dim diag = context.Diagnostics.First()
        Dim sourceSpan = diag.Location.SourceSpan
        Dim type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassStatementSyntax)().First()
        context.RegisterFix(CodeAction.Create("Mark as sealed", Function(ct) MarkClassAsSealed(context.Document, type, ct)), diag)
    End Function

    Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
        Return ImmutableArray.Create(PerformanceDiagnostics.SealedAttributeId)
    End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
        Return WellKnownFixAllProviders.BatchFixer
    End Function

    Private Async Function MarkClassAsSealed(document As Document, type As VisualBasic.Syntax.ClassStatementSyntax, cancellationToken As Threading.CancellationToken) As Task(Of Document)
        Return document.
            WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).
            ReplaceNode(type,
                        type.WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))).
                        WithAdditionalAnnotations(Formatting.Formatter.Annotation)))

    End Function
End Class
