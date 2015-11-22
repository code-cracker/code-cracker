Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Performance
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(RemoveWhereWhenItIsPossibleCodeFixProvider)), Composition.Shared>
    Public Class RemoveWhereWhenItIsPossibleCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(RemoveWhereWhenItIsPossibleAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            Dim name = diagnostic.Properties!methodName
            Dim message = $"Remove 'Where' moving predicate to '{name}'"
            context.RegisterCodeFix(CodeAction.Create(message, Function(c) RemoveWhere(context.Document, diagnostic, c), NameOf(RemoveWhereWhenItIsPossibleCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function RemoveWhere(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            Dim whereInvoke = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType(Of InvocationExpressionSyntax)().First()
            Dim nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()

            Dim whereMemberAccess = whereInvoke.ChildNodes.OfType(Of MemberAccessExpressionSyntax)().FirstOrDefault()
            Dim nextMethodMemberAccess = nextMethodInvoke.ChildNodes.OfType(Of MemberAccessExpressionSyntax)().FirstOrDefault()

            ' We need to push the args into the next invoke's arg list instead of just replacing
            ' where with new method because next method's arg list's end paren may have the CRLF which is dropped otherwise.
            Dim whereArgs = whereInvoke.ArgumentList
            Dim newArguments = nextMethodInvoke.ArgumentList.WithArguments(whereArgs.Arguments)

            Dim newNextMethodInvoke = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, whereMemberAccess.Expression, SyntaxFactory.Token(SyntaxKind.DotToken), nextMethodMemberAccess.Name), newArguments)

            Dim newRoot = root.ReplaceNode(nextMethodInvoke, newNextMethodInvoke)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class
End Namespace