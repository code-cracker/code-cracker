Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Reliability
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class UseConfigureAwaitFalseAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Use ConfigureAwait(False) on awaited task."
        Friend Const MessageFormat = "Consider using ConfigureAwait(False) on the awaited task."
        Friend Const Category = SupportedCategories.Reliability

        Friend Shared Rule As New DiagnosticDescriptor(
        DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId(),
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Hidden,
        isEnabledByDefault:=True,
        helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.UseConfigureAwaitFalse))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.AwaitExpression)
        End Sub

        Private Shared Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim awaitExpression = DirectCast(context.Node, AwaitExpressionSyntax)
            Dim awaitedExpression = awaitExpression.Expression
            If Not IsTask(awaitedExpression, context) Then Exit Sub

            Dim diag = Diagnostic.Create(Rule, awaitExpression.GetLocation())
            context.ReportDiagnostic(diag)
        End Sub

        Private Shared Function IsTask(expression As ExpressionSyntax, context As SyntaxNodeAnalysisContext) As Boolean
            Dim type = TryCast(context.SemanticModel.GetTypeInfo(expression).Type, INamedTypeSymbol)
            If type Is Nothing Then Return False
            Dim taskType As INamedTypeSymbol
            If type.IsGenericType Then
                type = type.ConstructedFrom
                taskType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")
            Else
                taskType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")
            End If
            Return type.Equals(taskType)
        End Function
    End Class
End Namespace