Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class PreferAnyToCountGreaterThanZeroAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(Properties.Resources.PreferAnyToCountGreaterThanZeroAnalyzer_Title), Properties.Resources.ResourceManager, GetType(Properties.Resources))
        Friend Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(Properties.Resources.PreferAnyToCountGreaterThanZeroAnalyzer_MessageFormat), Properties.Resources.ResourceManager, GetType(Properties.Resources))
        Friend Const Category = SupportedCategories.Style

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.PreferAnyToCountGreaterThanZero)
        )

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.GreaterThanExpression)
        End Sub

        Private Shared Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim binExpression = TryCast(context.Node, BinaryExpressionSyntax)
            If Not binExpression.IsKind(SyntaxKind.GreaterThanExpression) Then Return
            Dim rightSideExpression = TryCast(binExpression.Right, LiteralExpressionSyntax)
            If rightSideExpression Is Nothing Then Return
            If Not rightSideExpression.IsKind(SyntaxKind.NumericLiteralExpression) Then Return
            If rightSideExpression.Token.ToString() <> "0" Then Return
            Dim memberExpression = binExpression.Left.DescendantNodesAndSelf().OfType(Of MemberAccessExpressionSyntax).FirstOrDefault()
            If memberExpression Is Nothing Then Return
            If memberExpression.Name.ToString() <> "Count" Then Return
            Dim memberSymbolInfo = context.SemanticModel.GetSymbolInfo(memberExpression)
            Dim namespaceName = memberSymbolInfo.Symbol.ContainingNamespace.ToString()
            If namespaceName <> "System.Linq" AndAlso namespaceName <> "System.Collections" AndAlso namespaceName <> "System.Collections.Generic" Then Return

            context.ReportDiagnostic(Diagnostic.Create(Rule, binExpression.GetLocation(), GetPredicateString(binExpression.Left)))
        End Sub

        Private Shared Function GetPredicateString(expression As ExpressionSyntax) As String
            Dim predicateString = ""
            If TypeOf expression Is InvocationExpressionSyntax Then
                Dim arguments = TryCast(expression, InvocationExpressionSyntax).ArgumentList
                predicateString = If(arguments?.Arguments Is Nothing, "", If(arguments.Arguments.Count > 0, "predicate", ""))
            End If
            Return predicateString
        End Function
    End Class
End Namespace