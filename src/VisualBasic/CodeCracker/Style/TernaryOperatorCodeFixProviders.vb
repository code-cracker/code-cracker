Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style
    <ExportCodeFixProvider("CodeCrackerTernaryOperatorWithReturnCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class TernaryOperatorWithReturnCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MultiLineIfBlockSyntax)
            If declaration Is Nothing Then Exit Function
            context.RegisterFix(CodeAction.Create("Change to ternary operator", Function(c) MakeTernaryAsync(context.Document, declaration, c)), diagnostic)
        End Function

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.TernaryOperator_Return.ToDiagnosticId())
        End Function
        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, ifBlock As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim ifReturn = TryCast(ifBlock.Statements.FirstOrDefault(), ReturnStatementSyntax)
            Dim elseReturn = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), ReturnStatementSyntax)
            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     ifReturn.Expression.WithoutTrailingTrivia(),
                                                                     elseReturn.Expression.WithoutTrailingTrivia()).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)
            Dim returnStatement = SyntaxFactory.ReturnStatement(ternary)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim newRoot = root.ReplaceNode(ifBlock, returnStatement)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class

    <ExportCodeFixProvider("CodeCrackerTernaryOperatorWithAssignentCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class TernaryOperatorWithAssignmentCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MultiLineIfBlockSyntax)
            If declaration Is Nothing Then Exit Function
            context.RegisterFix(CodeAction.Create("Change to ternary operator", Function(c) MakeTernaryAsync(context.Document, declaration, c)), diagnostic)
        End Function

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId())
        End Function
        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, ifBlock As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim ifAssign = TryCast(ifBlock.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim elseAssign = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim variableIdentifier = TryCast(ifAssign.Left, IdentifierNameSyntax)

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(
                ifBlock.IfStatement.Condition,
                ifAssign.Right.WithoutTrailingTrivia(),
                elseAssign.Right.WithoutTrailingTrivia())

            Dim assignment = SyntaxFactory.SimpleAssignmentStatement(variableIdentifier, ternary).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim newRoot = root.ReplaceNode(ifBlock, assignment)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class

    <ExportCodeFixProvider("CodeCrackerTernaryOperatorFromIifCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class TernaryOperatorFromIifCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)
            If declaration Is Nothing Then Exit Function
            context.RegisterFix(CodeAction.Create("Change IIF to If to short circuit evaulations", Function(c) MakeTernaryAsync(context.Document, declaration, c)), diagnostic)
        End Function

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.TernaryOperator_Iif.ToDiagnosticId())
        End Function
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