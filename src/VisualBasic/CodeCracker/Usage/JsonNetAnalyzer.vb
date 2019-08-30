Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class JsonNetAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Your JSON syntax is wrong."
        Friend Const MessageFormat = "{0}"
        Private Const Description = "This diagnostic checks the Json string and triggers if the parsing fails by throwning an exception"

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.JsonNet.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Usage,
            SeverityConfigurations.Current(DiagnosticId.JsonNet),
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.JsonNet))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(c) Analyze(c, "DeserializeObject", "Public Shared Overloads Function DeserializeObject(Of T)(value As String) As T"), SyntaxKind.InvocationExpression)
            context.RegisterSyntaxNodeAction(Sub(c) Analyze(c, "Parse", "Public Shared Overloads Function Parse(json As String) As Newtonsoft.Json.Linq.JObject"), SyntaxKind.InvocationExpression)
        End Sub

        Private Sub Analyze(context As SyntaxNodeAnalysisContext, methodName As String, methodFullDefinition As String)
            If (context.IsGenerated()) Then Return
            Dim invocationExpression = DirectCast(context.Node, InvocationExpressionSyntax)
            Dim memberExpression = TryCast(invocationExpression.Expression, MemberAccessExpressionSyntax)
            If memberExpression?.Name?.Identifier.ValueText <> methodName Then Exit Sub

            Dim memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpression).Symbol
            If memberSymbol?.OriginalDefinition?.ToString() <> methodFullDefinition Then Exit Sub

            Dim argumentList = invocationExpression.ArgumentList
            If If(argumentList?.Arguments.Count, 0) <> 1 Then Exit Sub

            Dim literalParameter = TryCast(argumentList.Arguments(0).GetExpression(), LiteralExpressionSyntax)
            If literalParameter Is Nothing Then Exit Sub

            Dim jsonOpt = context.SemanticModel.GetConstantValue(literalParameter)
            Dim json = jsonOpt.Value.ToString()

            CheckJsonValue(context, literalParameter, json)
        End Sub

        Private Shared Sub CheckJsonValue(context As SyntaxNodeAnalysisContext, literalParameter As LiteralExpressionSyntax, json As String)
            Try
                parseMethodInfo.Value.Invoke(Nothing, {json})
            Catch ex As Exception
                Dim diag = Diagnostic.Create(Rule, literalParameter.GetLocation(), ex.InnerException.Message)
                context.ReportDiagnostic(diag)
            End Try
        End Sub

        Private Shared ReadOnly jObjectType As New Lazy(Of Type)(Function() System.Type.GetType("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json"))
        Private Shared ReadOnly parseMethodInfo As New Lazy(Of MethodInfo)(Function() jObjectType.Value.GetRuntimeMethod("Parse", {GetType(String)}))
    End Class
End Namespace
