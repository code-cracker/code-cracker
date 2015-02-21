Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider("CodeCrackerDisposablesShouldCallSuppressFinalizeCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class DisposablesShouldCallSuppressFinalizeCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim method = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MethodBlockSyntax)()
            context.RegisterFix(CodeAction.Create("Call GC.SuppressFinalize", Function(ct) AddSuppressFinalizeAsync(context.Document, method, ct)), diagnostic)
        End Function

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId())
        End Function

        Public Async Function AddSuppressFinalizeAsync(document As Document, method As MethodBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim suppressInvocation =
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.SimpleMemberAccessExpression(
                                    SyntaxFactory.IdentifierName("GC"),
                                    SyntaxFactory.IdentifierName("SuppressFinalize"))).
                            WithArgumentList(SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.SimpleArgument(SyntaxFactory.MeExpression)))).
                            WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim newMethod = SyntaxFactory.SubBlock(method.Begin).
                WithStatements(method.Statements.Add(suppressInvocation)).
                WithAdditionalAnnotations(Formatter.Annotation)

            Return document.
            WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).
                           ReplaceNode(method, newMethod).
                           WithAdditionalAnnotations(Formatter.Annotation))

        End Function
    End Class
End Namespace