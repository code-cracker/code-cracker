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
    <ExportCodeFixProvider("StringBuilderInLoopCodeFixProvider", LanguageNames.VisualBasic), Composition.Shared>
    Public Class StringBuilderInLoopCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(StringBuilderInLoopAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return Nothing
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim diagosticSpan = diagnostic.Location.SourceSpan
            Dim assignmentExpression = root.FindToken(diagosticSpan.Start).Parent.AncestorsAndSelf.OfType(Of AssignmentStatementSyntax).First
            context.RegisterCodeFix(CodeAction.Create("Use StringBuilder to create a value for " & assignmentExpression.Left.ToString(), Function(c) UseStringBuilder(context.Document, assignmentExpression, c)), diagnostic)

        End Function

        Private Async Function UseStringBuilder(document As Document, assignmentStatement As AssignmentStatementSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim expressionStatement = assignmentStatement
            Dim expressionStatementParent = expressionStatement.Parent
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim builderName = FindAvailableStringBuilderVariableName(assignmentStatement, semanticModel)
            Dim loopStatement = expressionStatement.FirstAncestorOrSelfOfType(
            GetType(WhileBlockSyntax),
            GetType(ForBlockSyntax),
            GetType(ForEachBlockSyntax),
            GetType(DoLoopBlockSyntax))

            Dim newExpressionStatementParent = ReplaceAddExpressionByStringBuilderAppendExpression(assignmentStatement, expressionStatement, expressionStatementParent, builderName)
            Dim newLoopStatement = loopStatement.ReplaceNode(expressionStatementParent, newExpressionStatementParent)
            Dim stringBuilderType = SyntaxFactory.ParseTypeName("System.Text.StringBuilder").WithAdditionalAnnotations(Simplifier.Annotation)

            Dim declarators As New SeparatedSyntaxList(Of VariableDeclaratorSyntax)()
            declarators = declarators.Add(SyntaxFactory.VariableDeclarator(New SeparatedSyntaxList(Of ModifiedIdentifierSyntax)().Add(SyntaxFactory.ModifiedIdentifier(builderName)),
             SyntaxFactory.AsNewClause(SyntaxFactory.ObjectCreationExpression(stringBuilderType).WithArgumentList(SyntaxFactory.ArgumentList())),
             Nothing))

            Dim stringBuilderDeclaration = SyntaxFactory.LocalDeclarationStatement(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.DimKeyword)),
                                                                               declarators).NormalizeWhitespace(" ").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim appendExpressionOnInitialization = SyntaxFactory.ParseExecutableStatement(builderName & ".Append(" & assignmentStatement.Left.ToString() & ")").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed) '.WithLeadingTrivia(assignmentStatement.GetLeadingTrivia()).WithTrailingTrivia(assignmentStatement.GetTrailingTrivia())
            Dim stringBuilderToString = SyntaxFactory.ParseExecutableStatement(assignmentStatement.Left.ToString() & " = " & builderName & ".ToString()").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed) '.WithLeadingTrivia(assignmentStatement.GetLeadingTrivia()).WithTrailingTrivia(assignmentStatement.GetTrailingTrivia())

            Dim loopParent = loopStatement.Parent
            Dim newLoopParent = loopParent.ReplaceNode(loopStatement,
                                                   {stringBuilderDeclaration, appendExpressionOnInitialization, newLoopStatement, stringBuilderToString}).
                                                   WithAdditionalAnnotations(Formatter.Annotation)
            Dim root = Await document.GetSyntaxRootAsync()
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