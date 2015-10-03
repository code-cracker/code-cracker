Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TernaryOperatorWithReturnCodeFixProvider)), Composition.Shared>
    Public Class TernaryOperatorWithReturnCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", Function(c) MakeTernaryAsync(context.Document, diagnostic, c), NameOf(TernaryOperatorWithReturnCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Return.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim span = diagnostic.Location.SourceSpan

            Dim ifBlock = root.FindToken(span.Start).Parent.FirstAncestorOrSelfOfType(Of MultiLineIfBlockSyntax)

            Dim ifReturn = TryCast(ifBlock.Statements.FirstOrDefault(), ReturnStatementSyntax)
            Dim elseReturn = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), ReturnStatementSyntax)
            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     ifReturn.Expression.WithoutTrailingTrivia(),
                                                                     elseReturn.Expression.WithoutTrailingTrivia()).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)
            Dim returnStatement = SyntaxFactory.ReturnStatement(ternary)

            Dim newRoot = root.ReplaceNode(ifBlock, returnStatement)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TernaryOperatorWithAssignmentCodeFixProvider)), Composition.Shared>
    Public Class TernaryOperatorWithAssignmentCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", Function(c) MakeTernaryAsync(context.Document, diagnostic, c), NameOf(TernaryOperatorWithAssignmentCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String) =
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim ifBlock = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of MultiLineIfBlockSyntax)

            Dim ifAssign = TryCast(ifBlock.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim elseAssign = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), AssignmentStatementSyntax)

            Dim assignment As AssignmentStatementSyntax
            If ifAssign.Left.Kind = SyntaxKind.IdentifierName Then
                assignment = CreateIdentifier(ifBlock, ifAssign, elseAssign)
            ElseIf ifAssign.Left.Kind = SyntaxKind.SimpleMemberAccessExpression
                assignment = CreateMemberAssignment(ifBlock, ifAssign, elseAssign)
            Else
                Return document
            End If

            Dim newRoot = root.ReplaceNode(ifBlock, assignment)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function

        Private Shared Function CreateIdentifier(ifBlock As MultiLineIfBlockSyntax, ifAssign As AssignmentStatementSyntax, elseAssign As AssignmentStatementSyntax) As AssignmentStatementSyntax
            Dim variableIdentifier = TryCast(ifAssign.Left, IdentifierNameSyntax)

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(
                ifBlock.IfStatement.Condition,
                ifAssign.Right.WithoutTrailingTrivia(),
                elseAssign.Right.WithoutTrailingTrivia())

            Dim assignment = SyntaxFactory.SimpleAssignmentStatement(variableIdentifier, ternary).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)
            Return assignment
        End Function

        Private Shared Function CreateMemberAssignment(ifBlock As MultiLineIfBlockSyntax, ifAssign As AssignmentStatementSyntax, elseAssign As AssignmentStatementSyntax) As AssignmentStatementSyntax
            Dim variableIdentifier = TryCast(ifAssign.Left, MemberAccessExpressionSyntax)

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(
                ifBlock.IfStatement.Condition,
                ifAssign.Right.WithoutTrailingTrivia(),
                elseAssign.Right.WithoutTrailingTrivia())

            Dim assignment = SyntaxFactory.SimpleAssignmentStatement(variableIdentifier, ternary).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Return assignment
        End Function
    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TernaryOperatorFromIifCodeFixProvider)), Composition.Shared>
    Public Class TernaryOperatorFromIifCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)
            If declaration Is Nothing Then Exit Function
            context.RegisterCodeFix(CodeAction.Create("Change IIF to If to short circuit evaulations", Function(c) MakeTernaryAsync(context.Document, declaration, c), NameOf(TernaryOperatorFromIifCodeFixProvider)), diagnostic)
        End Function

        Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String) =
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Iif.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, iifAssignment As InvocationExpressionSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim ternary = SyntaxFactory.TernaryConditionalExpression(
                iifAssignment.ArgumentList.Arguments(0).GetExpression(),
                iifAssignment.ArgumentList.Arguments(1).GetExpression(),
                iifAssignment.ArgumentList.Arguments(2).GetExpression()).
                WithLeadingTrivia(iifAssignment.GetLeadingTrivia()).
                WithTrailingTrivia(iifAssignment.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim newRoot = root.ReplaceNode(iifAssignment, ternary)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class
End Namespace