Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeFixProvider("StringBuilderInLoopCodeFixProvider", LanguageNames.VisualBasic)>
<[Shared]>
Public Class StringBuilderInLoopCodeFixProvider
    Inherits CodeFixProvider

    Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
        Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
        Dim diagnostic = context.Diagnostics.First
        Dim diagosticSpan = diagnostic.Location.SourceSpan
        Dim assignmentExpression = root.FindToken(diagosticSpan.Start).Parent.AncestorsAndSelf.OfType(Of AssignmentStatementSyntax).First
        context.RegisterFix(CodeAction.Create("Use StringBuilder to create a value for " & assignmentExpression.Left.ToString(), Function(c) UseStringBuilder(context.Document, assignmentExpression, c)), diagnostic)

    End Function

    Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
        Return ImmutableArray.Create(PerformanceDiagnostics.StringBuilderInLoop)
    End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
        Return Nothing
    End Function

    Public Sub foo()
        Dim myString = ""
        Dim builder As New System.Text.StringBuilder
        builder.Append(myString)
        For i As Integer = 1 To 10
            builder.Append("a")
            Exit For
        Next
        myString = builder.ToString()
        ' VB Requires value to be used or another analyzer is added which breaks the tests
    End Sub

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

        Dim modifiers As SyntaxTokenList = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.DimKeyword).WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" ")))
        Dim names() As ModifiedIdentifierSyntax = {SyntaxFactory.ModifiedIdentifier(builderName).WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" "))}
        Dim identifiers = SyntaxFactory.SeparatedList(Of ModifiedIdentifierSyntax)(names)

        Dim builder As New System.Text.StringBuilder()
        Dim emptyAttributeList As New SyntaxList(Of AttributeListSyntax)()

        Dim declarator = SyntaxFactory.VariableDeclarator(identifiers,
                                                          SyntaxFactory.AsNewClause(SyntaxFactory.ObjectCreationExpression(emptyAttributeList, stringBuilderType, SyntaxFactory.ArgumentList(), Nothing)),
                                                          Nothing).WithTrailingTrivia(SyntaxFactory.LineFeed)

        Dim declarators As New SeparatedSyntaxList(Of VariableDeclaratorSyntax)()
        declarators = declarators.Add(declarator)

        Dim stringBuilderDeclaration = SyntaxFactory.LocalDeclarationStatement(modifiers, declarators)

        Dim appendExpressionOnInitialization = SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression(builderName & ".Append(" & assignmentStatement.Left.ToString() & ")")).WithTrailingTrivia(SyntaxFactory.LineFeed)
        Dim stringBuilderToString = SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression(assignmentStatement.Left.ToString() & " = " & builderName & ".ToString()")).WithTrailingTrivia(SyntaxFactory.LineFeed)

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
            SyntaxFactory.ParseExpression(builderName & ".Append(" & DirectCast(assignment.Right, BinaryExpressionSyntax).Right.ToString() & ")"),
            SyntaxFactory.ParseExpression(builderName & ".Append(" & assignment.Right.ToString() & ")"))

        Dim invokeExp = SyntaxFactory.InvocationExpression(appendExpressionOnLoop).
            WithLeadingTrivia(expressionStatement.GetLeadingTrivia()).
            WithTrailingTrivia(expressionStatement.GetTrailingTrivia()).
            WithTrailingTrivia(SyntaxFactory.LineFeed)

        Dim invokeStatement = SyntaxFactory.ExpressionStatement(invokeExp)

        Dim newExpressionStatementParent = expressionStatementParent.ReplaceNode(expressionStatement, invokeStatement)
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
