Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Refactoring
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class ChangeAnyToAllAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const MessageAny = "Change Any to All"
        Friend Const MessageAll = "Change All to Any"
        Friend Const TitleAny = MessageAny
        Friend Const TitleAll = MessageAll
        Friend Const Category = SupportedCategories.Refactoring

        Friend Shared RuleAny As New DiagnosticDescriptor(
            DiagnosticId.ChangeAnyToAll.ToDiagnosticId(),
            TitleAny,
            MessageAny,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.ChangeAnyToAll))
        Friend Shared RuleAll As New DiagnosticDescriptor(
            DiagnosticId.ChangeAllToAny.ToDiagnosticId(),
            TitleAll,
            MessageAll,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.ChangeAllToAny))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(RuleAll, RuleAny)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeInvocation, SyntaxKind.InvocationExpression)
        End Sub

        Public Shared ReadOnly allName As IdentifierNameSyntax = SyntaxFactory.IdentifierName(NameOf(System.Linq.Enumerable.All))
        Public Shared ReadOnly anyName As IdentifierNameSyntax = SyntaxFactory.IdentifierName(NameOf(System.Linq.Enumerable.Any))

        Private Sub AnalyzeInvocation(context As SyntaxNodeAnalysisContext)
            If context.IsGenerated() Then Exit Sub
            Dim invocation = DirectCast(context.Node, InvocationExpressionSyntax)
            If invocation.Parent.IsKind(SyntaxKind.ExpressionStatement) Then Exit Sub
            Dim diagnosticToRaise = GetCorrespondingDiagnostic(context.SemanticModel, invocation)
            If diagnosticToRaise Is Nothing Then Exit Sub
            Dim diag = Diagnostic.Create(diagnosticToRaise, DirectCast(invocation.Expression, MemberAccessExpressionSyntax).Name.GetLocation())
            context.ReportDiagnostic(diag)
        End Sub

        Private Shared Function GetCorrespondingDiagnostic(model As SemanticModel, invocation As InvocationExpressionSyntax) As DiagnosticDescriptor
            Dim methodName = TryCast(invocation?.Expression, MemberAccessExpressionSyntax)?.Name?.ToString()
            Dim nameToCheck = If(methodName = NameOf(System.Linq.Enumerable.Any), allName,
                If(methodName = NameOf(System.Linq.Enumerable.All), anyName, Nothing))
            If nameToCheck Is Nothing Then Return Nothing
            Dim invocationSymbol = TryCast(model.GetSymbolInfo(invocation).Symbol, IMethodSymbol)
            If invocationSymbol?.Parameters.Length <> 1 Then Return Nothing
            If Not IsLambdaWithEmptyOrSimpleBody(invocation) Then Return Nothing
            Dim otherInvocation = invocation.WithExpression(DirectCast(invocation.Expression, MemberAccessExpressionSyntax).WithName(nameToCheck))
            Dim otherInvocationSymbol = model.GetSpeculativeSymbolInfo(invocation.SpanStart, otherInvocation, SpeculativeBindingOption.BindAsExpression)
            If otherInvocationSymbol.Symbol Is Nothing Then Return Nothing
            Return If(methodName = NameOf(System.Linq.Enumerable.Any), RuleAny, RuleAll)
        End Function

        Private Shared Function IsLambdaWithEmptyOrSimpleBody(invocation As InvocationExpressionSyntax) As Boolean
            Dim arg = invocation.ArgumentList?.Arguments.FirstOrDefault()
            If arg Is Nothing Then Return True ' Empty body
            Return arg.GetExpression().Kind = SyntaxKind.SingleLineFunctionLambdaExpression
        End Function
    End Class
End Namespace
