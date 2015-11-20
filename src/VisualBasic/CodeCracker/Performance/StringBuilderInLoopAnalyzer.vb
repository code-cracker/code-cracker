Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable

Namespace Performance
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class StringBuilderInLoopAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.StringBuilderInLoop.ToDiagnosticId()
        Public Const Title As String = "Don't concatenate strings in loops"
        Public Const MessageFormat As String = "Don't concatenate '{0}' in a loop."
        Public Const Category As String = SupportedCategories.Performance
        Public Const Description As String = "Do not concatenate a string in a loop. It will allocate a lot of memory. Use a StringBuilder instead. It will require less allocation, less garbage collection work, less CPU cycles, and less overall time."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.StringBuilderInLoop))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyze, SyntaxKind.AddAssignmentStatement, SyntaxKind.ConcatenateAssignmentStatement, SyntaxKind.SimpleAssignmentStatement)
        End Sub

        Private Sub Analyze(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim assignmentExpression = DirectCast(context.Node, AssignmentStatementSyntax)
            Dim loopStatment = assignmentExpression.FirstAncestorOfType(
            GetType(WhileBlockSyntax),
            GetType(ForBlockSyntax),
            GetType(ForEachBlockSyntax),
            GetType(DoLoopBlockSyntax))

            If loopStatment Is Nothing Then Exit Sub
            Dim semanticModel = context.SemanticModel
            Dim symbolForAssignment = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol
            If symbolForAssignment Is Nothing Then Exit Sub
            If TypeOf symbolForAssignment Is IPropertySymbol AndAlso DirectCast(symbolForAssignment, IPropertySymbol).Type.Name <> "String" Then Exit Sub
            If TypeOf symbolForAssignment Is IFieldSymbol AndAlso DirectCast(symbolForAssignment, IFieldSymbol).Type.Name <> "String" Then Exit Sub
            If TypeOf symbolForAssignment Is IParameterSymbol AndAlso DirectCast(symbolForAssignment, IParameterSymbol).Type.Name <> "String" Then Exit Sub
            If TypeOf symbolForAssignment Is ILocalSymbol Then
                Dim localSymbol = DirectCast(symbolForAssignment, ILocalSymbol)
                If localSymbol.Type.SpecialType <> SpecialType.System_String Then Exit Sub
                ' Don't analyze string declared within the loop.
                If loopStatment.DescendantTokens(localSymbol.DeclaringSyntaxReferences(0).Span).Any() Then Exit Sub
            End If

            If assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                If (Not If(assignmentExpression.Right?.IsKind(SyntaxKind.AddExpression), False)) Then Exit Sub
                Dim identifierOnConcatExpression = TryCast(DirectCast(assignmentExpression.Right, BinaryExpressionSyntax).Left, IdentifierNameSyntax)
                If identifierOnConcatExpression Is Nothing Then Exit Sub
                Dim symbolOnIdentifierOnConcatExpression = semanticModel.GetSymbolInfo(identifierOnConcatExpression).Symbol
                If Not symbolForAssignment.Equals(symbolOnIdentifierOnConcatExpression) Then Exit Sub

            ElseIf Not assignmentExpression.IsKind(SyntaxKind.AddAssignmentStatement) AndAlso
                Not assignmentExpression.IsKind(SyntaxKind.ConcatenateAssignmentStatement) Then
                Exit Sub
            End If

            Dim assignmentExpressionLeft = assignmentExpression.Left.ToString()
            Dim props = New Dictionary(Of String, String) From {{NameOf(assignmentExpressionLeft), assignmentExpressionLeft}}.ToImmutableDictionary()
            Dim diag = Diagnostic.Create(Rule, assignmentExpression.GetLocation(), props, assignmentExpression.Left.ToString())
            context.ReportDiagnostic(diag)
        End Sub
    End Class
End Namespace