Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Performance
    <ExportCodeFixProvider("CodeCrackerSealedAttributeCodeFixProvider", LanguageNames.VisualBasic), Composition.Shared>
    Public Class SealedAttributeCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(SealedAttributeAnalyzer.DiagnosticId)
        End Function

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diag = context.Diagnostics.First()
            Dim sourceSpan = diag.Location.SourceSpan
            Dim type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassStatementSyntax)().First()
            context.RegisterFix(CodeAction.Create("Mark as NotInheritable", Function(ct) MarkClassAsSealed(context.Document, type, ct)), diag)
        End Function

        Private Async Function MarkClassAsSealed(document As Document, type As VisualBasic.Syntax.ClassStatementSyntax, cancellationToken As Threading.CancellationToken) As Task(Of Document)
            Return document.
            WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).
            ReplaceNode(type,
                        type.WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))).
                        WithAdditionalAnnotations(Formatting.Formatter.Annotation)))

        End Function
    End Class
End Namespace