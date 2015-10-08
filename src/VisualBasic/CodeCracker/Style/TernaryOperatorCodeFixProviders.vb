Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style

    Public MustInherit Class TernaryOperatorCodeFixProviderBase
        Inherits CodeFixProvider

        Protected Shared Function MakeTernaryOperand(expression As ExpressionSyntax, semanticModel As SemanticModel, type As ITypeSymbol, typeSyntax As TypeSyntax) As ExpressionSyntax
            If type?.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                Dim constValue = semanticModel.GetConstantValue(expression)
                If constValue.HasValue AndAlso constValue.Value Is Nothing Then
                    Return SyntaxFactory.DirectCastExpression(expression.WithoutTrailingTrivia(), typeSyntax)
                End If
            End If

            Return expression
        End Function
    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TernaryOperatorWithReturnCodeFixProvider)), Composition.Shared>
    Public Class TernaryOperatorWithReturnCodeFixProvider
        Inherits TernaryOperatorCodeFixProviderBase

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
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim type = semanticModel.GetTypeInfo(ifReturn.Expression).ConvertedType
            Dim typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, ifReturn.SpanStart))
            Dim trueExpression = MakeTernaryOperand(ifReturn.Expression, semanticModel, type, typeSyntax)
            Dim falseExpression = MakeTernaryOperand(elseReturn.Expression, semanticModel, type, typeSyntax)
            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     trueExpression.WithoutTrailingTrivia(),
                                                                     falseExpression.WithoutTrailingTrivia()).
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
        Inherits TernaryOperatorCodeFixProviderBase

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
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim ifBlock = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of MultiLineIfBlockSyntax)

            Dim ifAssign = TryCast(ifBlock.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim elseAssign = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim type = semanticModel.GetTypeInfo(ifAssign.Left).ConvertedType
            Dim typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, ifAssign.SpanStart))
            Dim trueExpression = MakeTernaryOperand(ifAssign.Right, semanticModel, type, typeSyntax)
            Dim falseExpression = MakeTernaryOperand(elseAssign.Right, semanticModel, type, typeSyntax)
            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     trueExpression.WithoutTrailingTrivia(),
                                                                     falseExpression.WithoutTrailingTrivia()).
                                                                     WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                                                                     WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                                                                     WithAdditionalAnnotations(Formatter.Annotation)

            Dim assignment = SyntaxFactory.SimpleAssignmentStatement(ifAssign.Left, ternary).
                WithLeadingTrivia(ifBlock.GetLeadingTrivia()).
                WithTrailingTrivia(ifBlock.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(ifBlock, assignment)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TernaryOperatorFromIifCodeFixProvider)), Composition.Shared>
    Public Class TernaryOperatorFromIifCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create("Change IIF to If to short circuit evaulations", Function(c) MakeTernaryAsync(context.Document, diagnostic, c), NameOf(TernaryOperatorFromIifCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String) =
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Iif.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function MakeTernaryAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim iifAssignment = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(
                iifAssignment.ArgumentList.Arguments(0).GetExpression(),
                iifAssignment.ArgumentList.Arguments(1).GetExpression(),
                iifAssignment.ArgumentList.Arguments(2).GetExpression()).
                WithLeadingTrivia(iifAssignment.GetLeadingTrivia()).
                WithTrailingTrivia(iifAssignment.GetTrailingTrivia()).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(iifAssignment, ternary)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function
    End Class
End Namespace