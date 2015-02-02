Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports CodeCracker.Extensions

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
            helpLink:=HelpLink.ForDiagnostic(DiagnosticId.StringBuilderInLoop))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyze, SyntaxKind.AddAssignmentStatement, SyntaxKind.ConcatenateAssignmentStatement, SyntaxKind.SimpleAssignmentStatement)
        End Sub

        Private Sub Analyze(context As SyntaxNodeAnalysisContext)
            Dim assignmentExpression = DirectCast(context.Node, AssignmentStatementSyntax)
            Dim loopStatment = assignmentExpression.FirstAncestorOfType(
            GetType(WhileBlockSyntax),
            GetType(ForBlockSyntax),
            GetType(ForEachBlockSyntax),
            GetType(DoLoopBlockSyntax))

            If loopStatment Is Nothing Then Exit Sub
            Dim semanticModel = context.SemanticModel
            Dim symbolForAssignment = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol
            If TypeOf symbolForAssignment Is IPropertySymbol AndAlso DirectCast(symbolForAssignment, IPropertySymbol).Type.Name <> "String" Then Exit Sub
            If TypeOf symbolForAssignment Is ILocalSymbol AndAlso DirectCast(symbolForAssignment, ILocalSymbol).Type.Name <> "String" Then Exit Sub
            If TypeOf symbolForAssignment Is IFieldSymbol AndAlso DirectCast(symbolForAssignment, IFieldSymbol).Type.Name <> "String" Then Exit Sub

            If assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                If (Not If(assignmentExpression.Right?.IsKind(SyntaxKind.AddExpression), False)) Then Exit Sub
                Dim identifierOnConcatExpression = TryCast(DirectCast(assignmentExpression.Right, BinaryExpressionSyntax).Left, IdentifierNameSyntax)
                If identifierOnConcatExpression Is Nothing Then Exit Sub
                Dim symbolOnIdentifierOnConcatExpression = semanticModel.GetSymbolInfo(identifierOnConcatExpression).Symbol
                If Not symbolForAssignment.Equals(symbolOnIdentifierOnConcatExpression) Then Exit Sub

            ElseIf Not assignmentExpression.IsKind(SyntaxKind.AddAssignmentStatement) Then
                Exit Sub
            End If

            Dim diag = Diagnostic.Create(Rule, assignmentExpression.GetLocation(), assignmentExpression.Left.ToString())
            context.ReportDiagnostic(diag)
        End Sub
    End Class
End Namespace