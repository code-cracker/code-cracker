Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Linq

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class NameOfAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.NameOf.ToDiagnosticId()
        Public Const Title As String = "You should use nameof instead of the parameter string"
        Public Const MessageFormat As String = "Use 'NameOf({0})' instead of specifying the parameter name."
        Public Const Category As String = SupportedCategories.Design
        Public Const Description As String = "The NameOf() operator should be used to specify the name of a parameter instead of a string literal as it produces code that is easier to refactor."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.NameOf))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(LanguageVersion.VisualBasic14, AddressOf Analyzer, SyntaxKind.StringLiteralExpression)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim stringLiteral = DirectCast(context.Node, LiteralExpressionSyntax)
            If String.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText) Then Return
            Dim parameters = GetParameters(stringLiteral)
            If Not parameters.Any() Then Return
            Dim attribute = stringLiteral.FirstAncestorOfType(Of AttributeSyntax)()
            Dim method = TryCast(stringLiteral.FirstAncestorOfType(GetType(MethodBlockSyntax), GetType(ConstructorBlockSyntax)), MethodBlockBaseSyntax)
            If attribute IsNot Nothing AndAlso method.BlockStatement.AttributeLists.Any(Function(a) a.Attributes.Contains(attribute)) Then Return
            If Not AreEqual(stringLiteral, parameters) Then Return
            Dim diag = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.Value)
            context.ReportDiagnostic(diag)
        End Sub

        Private Function AreEqual(stringLiteral As LiteralExpressionSyntax, paramaters As SeparatedSyntaxList(Of ParameterSyntax)) As Boolean
            Return paramaters.Any(Function(p) p.Identifier?.Identifier.ValueText = stringLiteral.Token.ValueText)
        End Function

        Public Function GetParameters(node As SyntaxNode) As SeparatedSyntaxList(Of ParameterSyntax)
            Dim methodDeclaration = node.FirstAncestorOfType(Of MethodBlockSyntax)()
            Dim parameters As SeparatedSyntaxList(Of ParameterSyntax)
            If methodDeclaration IsNot Nothing Then
                parameters = methodDeclaration.SubOrFunctionStatement.ParameterList.Parameters
            Else
                Dim constructorDeclaration = node.FirstAncestorOfType(Of ConstructorBlockSyntax)()
                If constructorDeclaration IsNot Nothing Then
                    parameters = constructorDeclaration.SubNewStatement.ParameterList.Parameters
                Else
                    Return New SeparatedSyntaxList(Of ParameterSyntax)()
                End If
            End If
            Return parameters
        End Function
    End Class
End Namespace