Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Performance
    <ExportCodeFixProvider("CodeCrackerRemoveWhereWhenItIsPossibleCodeFixProvider", LanguageNames.VisualBasic), Composition.Shared>
    Public Class RemoveWhereWhenItIsPossibleCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(RemoveWhereWhenItIsPossibleAnalyzer.DiagnosticId)
        End Function

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            Dim whereInvoke = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType(Of InvocationExpressionSyntax)().First()
            Dim nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()
            Dim message = "Remove 'Where' moving predicate to '" + RemoveWhereWhenItIsPossibleAnalyzer.GetNameOfTheInvokeMethod(nextMethodInvoke) + "'"
            context.RegisterFix(CodeAction.Create(message, Function(c) RemoveWhere(context.Document, whereInvoke, nextMethodInvoke, c)), diagnostic)
        End Function

        Private Async Function RemoveWhere(document As Document, whereInvoke As InvocationExpressionSyntax, nextMethodInvoke As InvocationExpressionSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync()
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