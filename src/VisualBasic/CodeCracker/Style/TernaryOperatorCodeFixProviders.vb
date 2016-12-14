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
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim type = GetCommonBaseType(semanticModel.GetTypeInfo(ifReturn.Expression).ConvertedType, semanticModel.GetTypeInfo(elseReturn.Expression).ConvertedType)

            Dim ifType = semanticModel.GetTypeInfo(ifReturn.Expression).Type
            Dim elseType = semanticModel.GetTypeInfo(elseReturn.Expression).Type

            Dim typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, ifReturn.SpanStart))
            Dim trueExpression = ifReturn.Expression.
                ConvertToBaseType(ifType, type).
                EnsureNothingAsType(semanticModel, type, typeSyntax)

            Dim falseExpression = elseReturn.Expression.
                ConvertToBaseType(elseType, type).
                EnsureNothingAsType(semanticModel, type, typeSyntax)

            Dim leadingTrivia = ifBlock.GetLeadingTrivia()
            leadingTrivia = leadingTrivia.InsertRange(leadingTrivia.Count - 1, ifReturn.GetLeadingTrivia())
            leadingTrivia = leadingTrivia.InsertRange(leadingTrivia.Count - 1, elseReturn.GetLeadingTrivia())

            Dim trailingTrivia = ifBlock.GetTrailingTrivia.
                InsertRange(0, elseReturn.GetTrailingTrivia().Where(Function(trivia) Not trivia.IsKind(SyntaxKind.EndOfLineTrivia))).
                InsertRange(0, ifReturn.GetTrailingTrivia().Where(Function(trivia) Not trivia.IsKind(SyntaxKind.EndOfLineTrivia)))

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     trueExpression.WithoutTrailingTrivia(),
                                                                     falseExpression.WithoutTrailingTrivia())

            Dim returnStatement = SyntaxFactory.ReturnStatement(ternary).
                WithLeadingTrivia(leadingTrivia).
                WithTrailingTrivia(trailingTrivia).
                WithAdditionalAnnotations(Formatter.Annotation)

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
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim ifBlock = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of MultiLineIfBlockSyntax)

            Dim ifAssign = TryCast(ifBlock.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim elseAssign = TryCast(ifBlock.ElseBlock?.Statements.FirstOrDefault(), AssignmentStatementSyntax)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim type = GetCommonBaseType(semanticModel.GetTypeInfo(ifAssign.Left).ConvertedType, semanticModel.GetTypeInfo(elseAssign.Left).ConvertedType)
            Dim typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, ifAssign.SpanStart))

            Dim ifType = semanticModel.GetTypeInfo(ifAssign.Right).Type
            Dim elseType = semanticModel.GetTypeInfo(elseAssign.Right).Type

            Dim trueExpression = ifAssign.
                ExtractAssignmentAsExpressionSyntax().
                EnsureNothingAsType(semanticModel, type, typeSyntax).
                ConvertToBaseType(ifType, type)

            Dim falseExpression = elseAssign.
                ExtractAssignmentAsExpressionSyntax().
                EnsureNothingAsType(semanticModel, type, typeSyntax).
                ConvertToBaseType(elseType, type)

            If ifAssign.OperatorToken.Text <> "=" AndAlso ifAssign.OperatorToken.Text = elseAssign.OperatorToken.Text Then
                trueExpression = ifAssign.Right.
                EnsureNothingAsType(semanticModel, type, typeSyntax).
                ConvertToBaseType(ifType, type)

                falseExpression = elseAssign.Right.
                EnsureNothingAsType(semanticModel, type, typeSyntax).
                ConvertToBaseType(elseType, type)
            End If

            Dim leadingTrivia = ifBlock.GetLeadingTrivia.
                AddRange(ifAssign.GetLeadingTrivia()).
                AddRange(trueExpression.GetLeadingTrivia()).
                AddRange(elseAssign.GetLeadingTrivia()).
                AddRange(falseExpression.GetLeadingTrivia())

            Dim trailingTrivia = ifBlock.GetTrailingTrivia.
                InsertRange(0, elseAssign.GetTrailingTrivia().Where(Function(trivia) Not trivia.IsKind(SyntaxKind.EndOfLineTrivia))).
                InsertRange(0, ifAssign.GetTrailingTrivia().Where(Function(trivia) Not trivia.IsKind(SyntaxKind.EndOfLineTrivia)))

            Dim ternary = SyntaxFactory.TernaryConditionalExpression(ifBlock.IfStatement.Condition.WithoutTrailingTrivia(),
                                                                     trueExpression.WithoutTrailingTrivia(),
                                                                     falseExpression.WithoutTrailingTrivia())

            Dim ternaryOperatorToken As SyntaxToken = If((ifAssign.OperatorToken.Text <> "=" OrElse elseAssign.OperatorToken.Text <> "=") AndAlso ifAssign.OperatorToken.Text <> elseAssign.OperatorToken.Text,
                                                          SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                                          ifAssign.OperatorToken)

            Dim assignment = SyntaxFactory.SimpleAssignmentStatement(ifAssign.Left.WithLeadingTrivia(leadingTrivia), ternaryOperatorToken, ternary).
                WithTrailingTrivia(trailingTrivia).
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