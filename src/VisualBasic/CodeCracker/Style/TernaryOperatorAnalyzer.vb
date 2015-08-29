Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class TernaryOperatorAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Use Ternary operator."
        Friend Const MessageFormat = "You can use a ternary operator."

        Friend Shared RuleForIfWithReturn As New DiagnosticDescriptor(
            DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Style,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.TernaryOperator_Return))

        Friend Shared RuleForIfWithAssignment As New DiagnosticDescriptor(
            DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Style,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.TernaryOperator_Assignment))

        Friend Shared RuleForIif As New DiagnosticDescriptor(
            DiagnosticId.TernaryOperator_Iif.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Style,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.TernaryOperator_Iif))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(RuleForIfWithAssignment, RuleForIfWithReturn, RuleForIif)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.IfStatement)
            context.RegisterSyntaxNodeAction(AddressOf IifAnalyzer, SyntaxKind.InvocationExpression)
        End Sub
        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim ifStatement = TryCast(context.Node, IfStatementSyntax)
            If ifStatement Is Nothing Then Exit Sub
            Dim ifBlock = TryCast(ifStatement.Parent, MultiLineIfBlockSyntax)
            If ifBlock Is Nothing Then Exit Sub
            If ifBlock.ElseBlock Is Nothing Then Exit Sub
            If ifBlock.Statements.Count <> 1 OrElse ifBlock.ElseBlock.Statements.Count <> 1 Then Exit Sub

            Dim ifClauseStatement = ifBlock.Statements(0)
            Dim elseStatement = ifBlock.ElseBlock.Statements(0)

            If TypeOf (ifClauseStatement) Is ReturnStatementSyntax AndAlso
            TypeOf (elseStatement) Is ReturnStatementSyntax Then

                Dim diag = Diagnostic.Create(RuleForIfWithReturn, ifStatement.IfKeyword.GetLocation, "You can use a ternary operator.")
                context.ReportDiagnostic(diag)
                Exit Sub
            End If

            Dim ifAssignment = TryCast(ifClauseStatement, AssignmentStatementSyntax)
            Dim elseAssignment = TryCast(elseStatement, AssignmentStatementSyntax)
            If ifAssignment Is Nothing OrElse elseAssignment Is Nothing Then Exit Sub
            If Not ifAssignment?.Left.IsEquivalentTo(elseAssignment?.Left) Then Exit Sub
            Dim assignDiag = Diagnostic.Create(RuleForIfWithAssignment, ifStatement.IfKeyword.GetLocation(), "You can use a ternary operator.")
            context.ReportDiagnostic(assignDiag)
        End Sub

        Private Sub IifAnalyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim iifStatement = TryCast(context.Node, InvocationExpressionSyntax)
            If iifStatement Is Nothing Then Exit Sub
            Dim iifExpression = TryCast(iifStatement.Expression, IdentifierNameSyntax)
            If iifExpression Is Nothing Then Exit Sub
            If String.Equals(iifExpression.Identifier.ValueText, "iif", StringComparison.OrdinalIgnoreCase) Then
                If iifStatement.ArgumentList.Arguments.Count = 3 Then
                    Dim diag = Diagnostic.Create(RuleForIif, iifStatement.GetLocation())
                    context.ReportDiagnostic(diag)
                End If
            End If
        End Sub
    End Class
End Namespace
