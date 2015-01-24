Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class NameOfAnalyzer
        Inherits DiagnosticAnalyzer

        Public Const DiagnosticId As String = "CC0021"
        Public Const Title As String = "You should use nameof instead of the parameter string"
        Public Const MessageFormat As String = "Use 'NameOf({0})' instead of specifying the parameter name."
        Public Const Category As String = SupportedCategories.Design
        Public Const Description As String = "The NameOf() operator should be used to specify the name of a parameter instead of a string literal as it produces code that is easier to refactor."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLink:=HelpLink.ForDiagnostic(DiagnosticId))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(LanguageVersion.VisualBasic14, AddressOf Analyzer, SyntaxKind.StringLiteralExpression)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            Dim stringLiteral = DirectCast(context.Node, LiteralExpressionSyntax)
            If Not String.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText) Then
                ' TODO: This appears to only return the first sub, not one that matches the string literal ensure this is working correctly with multiple items.
                Dim methodDeclaration = stringLiteral.AncestorsAndSelf().OfType(Of Syntax.MethodBlockSyntax).FirstOrDefault()
                If methodDeclaration IsNot Nothing Then
                    Dim methodParams = methodDeclaration.Begin.ParameterList.Parameters
                    If Not AreEqual(stringLiteral, methodParams) Then Exit Sub

                Else
                    Dim constructorDeclaration = stringLiteral.AncestorsAndSelf.OfType(Of ConstructorBlockSyntax).FirstOrDefault
                    If constructorDeclaration IsNot Nothing Then
                        Dim constructorParams = constructorDeclaration.Begin.ParameterList.Parameters
                        If Not AreEqual(stringLiteral, constructorParams) Then Exit Sub
                    Else
                        Exit Sub
                    End If
                End If
                Dim diag = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.Value)
                context.ReportDiagnostic(diag)
            End If

        End Sub

        Private Function AreEqual(stringLiteral As LiteralExpressionSyntax, paramaters As SeparatedSyntaxList(Of ParameterSyntax)) As Boolean
            Return paramaters.Any(Function(p) p.Identifier?.Identifier.ValueText = stringLiteral.Token.ValueText)
        End Function

    End Class
End Namespace