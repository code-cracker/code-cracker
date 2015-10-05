Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports System.Composition
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Formatting

Namespace Refactoring
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ChangeAnyToAllCodeFixProvider)), [Shared]>
    Public Class ChangeAnyToAllCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticId.ChangeAnyToAll.ToDiagnosticId(),
                                             DiagnosticId.ChangeAllToAny.ToDiagnosticId())
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diag = context.Diagnostics.First
            Dim message = If(diag.Id = DiagnosticId.ChangeAnyToAll.ToDiagnosticId, "Change Any to All", "Change All to Any")
            context.RegisterCodeFix(CodeAction.Create(message, Function(c) ConvertAsync(context.Document, diag.Location, c), NameOf(ChangeAnyToAllCodeFixProvider)), diag)
            Return Task.FromResult(0)
        End Function

        Private Shared Async Function ConvertAsync(Document As Document, diagnosticLocation As Location, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation = root.FindNode(diagnosticLocation.SourceSpan).FirstAncestorOfType(Of InvocationExpressionSyntax)
            Dim newInvocation = CreateNewInvocation(invocation).
                WithAdditionalAnnotations(Formatter.Annotation)
            Dim newRoot = ReplaceInvocation(invocation, newInvocation, root)
            Dim newDocument = Document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function

        Private Shared Function ReplaceInvocation(invocation As InvocationExpressionSyntax, newInvocation As ExpressionSyntax, root As SyntaxNode) As SyntaxNode
            If invocation.Parent.IsKind(SyntaxKind.NotExpression) Then
                Return root.ReplaceNode(invocation.Parent, newInvocation)
            End If
            Dim negatedInvocation = SyntaxFactory.NotExpression(newInvocation)
            Dim newRoot = root.ReplaceNode(invocation, negatedInvocation)
            Return newRoot
        End Function

        Friend Shared Function CreateNewInvocation(invocation As InvocationExpressionSyntax) As ExpressionSyntax
            Dim methodName = DirectCast(invocation.Expression, MemberAccessExpressionSyntax).Name.ToString
            Dim nameToCheck = If(methodName = NameOf(Enumerable.Any), ChangeAnyToAllAnalyzer.allName, ChangeAnyToAllAnalyzer.anyName)
            Dim newInvocation = invocation.WithExpression(DirectCast(invocation.Expression, MemberAccessExpressionSyntax).WithName(nameToCheck))
            Dim comparisonExpression = DirectCast(DirectCast(newInvocation.ArgumentList.Arguments.First().GetExpression(), SingleLineLambdaExpressionSyntax).Body, ExpressionSyntax)
            Dim newComparisonExpression = CreateNewComparison(comparisonExpression)
            newComparisonExpression = RemoveParenthesis(newComparisonExpression)
            newInvocation = newInvocation.ReplaceNode(comparisonExpression, newComparisonExpression)
            Return newInvocation
        End Function

        Private Shared Function CreateNewComparison(comparisonExpression As ExpressionSyntax) As ExpressionSyntax
            If comparisonExpression.IsKind(SyntaxKind.TernaryConditionalExpression) Then
                Return SyntaxFactory.EqualsExpression(comparisonExpression,
                                                      SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
            End If
            If comparisonExpression.IsKind(SyntaxKind.NotExpression) Then
                Return DirectCast(comparisonExpression, UnaryExpressionSyntax).Operand
            End If

            If comparisonExpression.IsKind(SyntaxKind.EqualsExpression) Then
                Dim binaryComparison = DirectCast(comparisonExpression, BinaryExpressionSyntax)
                If binaryComparison.Right.IsKind(SyntaxKind.TrueLiteralExpression) Then
                    Return binaryComparison.WithRight(SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
                End If
                If binaryComparison.Left.IsKind(SyntaxKind.TrueLiteralExpression) Then
                    Return binaryComparison.WithLeft(SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
                End If
                If binaryComparison.Right.IsKind(SyntaxKind.FalseLiteralExpression) Then
                    Return binaryComparison.Left
                End If
                If binaryComparison.Left.IsKind(SyntaxKind.FalseLiteralExpression) Then
                    Return binaryComparison.Right
                End If
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.NotEqualsExpression, SyntaxFactory.Token(SyntaxKind.LessThanGreaterThanToken))
            End If

            If comparisonExpression.IsKind(SyntaxKind.NotEqualsExpression) Then
                Dim binaryComparison = DirectCast(comparisonExpression, BinaryExpressionSyntax)
                If binaryComparison.Right.IsKind(SyntaxKind.TrueLiteralExpression) Then
                    Return binaryComparison.Left
                End If
                If binaryComparison.Left.IsKind(SyntaxKind.TrueLiteralExpression) Then
                    Return binaryComparison.Right
                End If
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.EqualsExpression, SyntaxFactory.Token(SyntaxKind.EqualsToken))
            End If
            If comparisonExpression.IsKind(SyntaxKind.GreaterThanExpression) Then
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.LessThanOrEqualExpression, SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken))
            End If
            If comparisonExpression.IsKind(SyntaxKind.GreaterThanOrEqualExpression) Then
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.LessThanExpression, SyntaxFactory.Token(SyntaxKind.LessThanToken))
            End If
            If comparisonExpression.IsKind(SyntaxKind.LessThanExpression) Then
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.GreaterThanOrEqualExpression, SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken))
            End If
            If comparisonExpression.IsKind(SyntaxKind.LessThanOrEqualExpression) Then
                Return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.GreaterThanExpression, SyntaxFactory.Token(SyntaxKind.GreaterThanToken))
            End If
            If comparisonExpression.IsKind(SyntaxKind.TrueLiteralExpression) Then
                Return SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword))
            End If
            If comparisonExpression.IsKind(SyntaxKind.FalseLiteralExpression) Then
                Return SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword))
            End If
            Return SyntaxFactory.EqualsExpression(comparisonExpression,
                                                  SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
        End Function

        Private Shared Function RemoveParenthesis(expression As ExpressionSyntax) As ExpressionSyntax
            Return If(expression.IsKind(SyntaxKind.ParenthesizedExpression),
                DirectCast(expression, ParenthesizedExpressionSyntax).Expression,
                expression)
        End Function

        Private Shared Function CreateNewBinaryExpression(comparisonExpression As ExpressionSyntax, kind As SyntaxKind, operatorToken As SyntaxToken) As BinaryExpressionSyntax
            Dim binaryComparison = DirectCast(comparisonExpression, BinaryExpressionSyntax)
            Dim left = binaryComparison.Left
            Dim newComparison = SyntaxFactory.BinaryExpression(
                kind,
                If(left.IsKind(SyntaxKind.BinaryConditionalExpression), SyntaxFactory.ParenthesizedExpression(left), left),
                operatorToken,
                binaryComparison.Right)
            Return newComparison
        End Function
    End Class
End Namespace
