Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Performance
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(StringBuilderInLoopCodeFixProvider)), Composition.Shared>
    Public Class StringBuilderInLoopCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(StringBuilderInLoopAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return Nothing
        End Function

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create($"Use StringBuilder to create a value for '{diagnostic.Properties!assignmentExpressionLeft}'", Function(c) UseStringBuilder(context.Document, diagnostic, c), NameOf(StringBuilderInLoopCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function UseStringBuilder(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim diagosticSpan = diagnostic.Location.SourceSpan
            Dim expressionStatement = root.FindToken(diagosticSpan.Start).Parent.AncestorsAndSelf.OfType(Of AssignmentStatementSyntax).First


            Dim expressionStatementParent = expressionStatement.Parent
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim builderName = FindAvailableStringBuilderVariableName(expressionStatement, semanticModel)
            Dim loopStatement = expressionStatement.FirstAncestorOrSelfOfType(
            GetType(WhileBlockSyntax),
            GetType(ForBlockSyntax),
            GetType(ForEachBlockSyntax),
            GetType(DoLoopBlockSyntax))

            Dim newExpressionStatementParent = ReplaceAddExpressionByStringBuilderAppendExpression(expressionStatement, expressionStatement, expressionStatementParent, builderName)
            Dim newLoopStatement = loopStatement.ReplaceNode(expressionStatementParent, newExpressionStatementParent)
            Dim stringBuilderType = SyntaxFactory.ParseTypeName("System.Text.StringBuilder").WithAdditionalAnnotations(Simplifier.Annotation)

            Dim declarators As New SeparatedSyntaxList(Of VariableDeclaratorSyntax)()
            declarators = declarators.Add(SyntaxFactory.VariableDeclarator(New SeparatedSyntaxList(Of ModifiedIdentifierSyntax)().Add(SyntaxFactory.ModifiedIdentifier(builderName)),
             SyntaxFactory.AsNewClause(SyntaxFactory.ObjectCreationExpression(stringBuilderType).WithArgumentList(SyntaxFactory.ArgumentList())),
             Nothing))

            Dim stringBuilderDeclaration = SyntaxFactory.LocalDeclarationStatement(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.DimKeyword)),
                                                                               declarators).NormalizeWhitespace(" ").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim appendExpressionOnInitialization = SyntaxFactory.ParseExecutableStatement(builderName & ".Append(" & expressionStatement.Left.ToString() & ")").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            Dim stringBuilderToString = SyntaxFactory.ParseExecutableStatement(expressionStatement.Left.ToString() & " = " & builderName & ".ToString()").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim loopParent = loopStatement.Parent
            Dim newLoopParent = loopParent.ReplaceNode(loopStatement,
                                                   {stringBuilderDeclaration, appendExpressionOnInitialization, newLoopStatement, stringBuilderToString}).
                                                   WithAdditionalAnnotations(Formatter.Annotation)
            Dim newroot = root.ReplaceNode(loopParent, newLoopParent)
            Dim newDocument = document.WithSyntaxRoot(newroot)
            Return newDocument
        End Function

        Private Shared Function ReplaceAddExpressionByStringBuilderAppendExpression(assignment As AssignmentStatementSyntax, expressionStatement As SyntaxNode, expressionStatementParent As SyntaxNode, builderName As String) As SyntaxNode
            Dim appendExpressionOnLoop = If(assignment.IsKind(SyntaxKind.SimpleAssignmentStatement),
            SyntaxFactory.ParseExecutableStatement(builderName & ".Append(" & DirectCast(assignment.Right, BinaryExpressionSyntax).Right.ToString() & ")"),
            SyntaxFactory.ParseExecutableStatement(builderName & ".Append(" & assignment.Right.ToString() & ")")).
                WithLeadingTrivia(assignment.GetLeadingTrivia()).
                WithTrailingTrivia(assignment.GetTrailingTrivia())

            Dim newExpressionStatementParent = expressionStatementParent.ReplaceNode(expressionStatement, appendExpressionOnLoop)
            Return newExpressionStatementParent
        End Function

        Private Shared Function FindAvailableStringBuilderVariableName(assignmentStatement As AssignmentStatementSyntax, semanticModel As SemanticModel) As String
            Const builderNameBase = "builder"
            Dim builderName = builderNameBase
            Dim builderNameIncrementer = 0
            While semanticModel.LookupSymbols(assignmentStatement.GetLocation().SourceSpan.Start, name:=builderName).Any()
                builderNameIncrementer += 1
                builderName = builderNameBase & builderNameIncrementer.ToString()
            End While
            Return builderName
        End Function
    End Class
End Namespace