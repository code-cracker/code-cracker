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
        Public Const Title As String = "You should use nameof instead of the parameter element name string"
        Public Const MessageFormat As String = "Use 'NameOf({0})' instead of specifying the program element name."
        Public Const Category As String = SupportedCategories.Design
        Public Const Description As String = "In VB14 the NameOf() operator should be used to specify the name of a program element instead of a string literal as it produces code that is easier to refactor."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.NameOf))
        Protected Shared RuleExtenal As DiagnosticDescriptor = New DiagnosticDescriptor(
            DiagnosticId.NameOf_External.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.NameOf_External))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule, RuleExtenal)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(LanguageVersion.VisualBasic14, AddressOf Analyzer, SyntaxKind.StringLiteralExpression)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim stringLiteral = DirectCast(context.Node, LiteralExpressionSyntax)
            If String.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText) Then Return

            Dim externalSymbol = False
            Dim programElementName = GetProgramElementNameThatMatchesStringLiteral(stringLiteral, context.SemanticModel, externalSymbol)
            If (Found(programElementName)) Then
                Dim diag = Diagnostic.Create(If(externalSymbol, RuleExtenal, Rule), stringLiteral.GetLocation(), programElementName)
                context.ReportDiagnostic(diag)
            End If
        End Sub

        Private Shared Function GetProgramElementNameThatMatchesStringLiteral(stringLiteral As LiteralExpressionSyntax, model As SemanticModel, ByRef externalSymbol As Boolean) As String
            Dim programElementName = GetParameterNameThatMatchesStringLiteral(stringLiteral)
            If Not Found(programElementName) Then
                Dim literalValueText = stringLiteral.Token.ValueText
                Dim symbol = model.LookupSymbols(stringLiteral.Token.SpanStart, Nothing, literalValueText).FirstOrDefault()
                If symbol Is Nothing Then Return Nothing
                externalSymbol = symbol.Locations.Any(Function(l) l.IsInSource) = False
                If symbol.Kind = SymbolKind.Local Then
                    ' Only register if local variable is declared before it is used.
                    ' Don't recommend if variable is declared after string literal is used.
                    Dim symbolSpan = symbol.Locations.Min(Function(i) i.SourceSpan)
                    If symbolSpan.CompareTo(stringLiteral.Token.Span) > 0 Then
                        Return Nothing
                    End If
                End If
                programElementName = symbol?.ToDisplayParts().
                    Where(AddressOf IncludeOnlyPartsThatAreName).
                    LastOrDefault(Function(displayPart) displayPart.ToString() = literalValueText).
                    ToString()
            End If
            Return programElementName
        End Function

        Private Shared Function GetParameterNameThatMatchesStringLiteral(stringLiteral As LiteralExpressionSyntax) As String
            Dim ancestorThatMightHaveParameters = stringLiteral.FirstAncestorOfType(GetType(AttributeListSyntax), GetType(MethodBlockSyntax), GetType(SubNewStatementSyntax), GetType(InvocationExpressionSyntax))
            Dim parameterName = String.Empty
            If ancestorThatMightHaveParameters IsNot Nothing Then
                Dim parameters = New SeparatedSyntaxList(Of ParameterSyntax)()
                Select Case ancestorThatMightHaveParameters.Kind
                    Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock
                        Dim method = DirectCast(ancestorThatMightHaveParameters, MethodBlockSyntax)
                        parameters = method.SubOrFunctionStatement.ParameterList.Parameters
                    Case SyntaxKind.AttributeList
                End Select
                parameterName = GetParameterWithIdentifierEqualToStringLiteral(stringLiteral, parameters)?.Identifier.Identifier.Text

            End If
            Return parameterName
        End Function

        Private Shared Function Found(programElement As String) As Boolean
            Return Not String.IsNullOrEmpty(programElement)
        End Function

        Public Shared Function IncludeOnlyPartsThatAreName(displayPart As SymbolDisplayPart) As Boolean
            Return displayPart.IsAnyKind(SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.DelegateName, SymbolDisplayPartKind.EnumName, SymbolDisplayPartKind.EventName, SymbolDisplayPartKind.FieldName, SymbolDisplayPartKind.InterfaceName, SymbolDisplayPartKind.LocalName, SymbolDisplayPartKind.MethodName, SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.ParameterName, SymbolDisplayPartKind.PropertyName, SymbolDisplayPartKind.StructName)
        End Function

        Private Shared Function GetParameterWithIdentifierEqualToStringLiteral(stringLiteral As LiteralExpressionSyntax, parameters As SeparatedSyntaxList(Of ParameterSyntax)) As ParameterSyntax
            Return parameters.FirstOrDefault(Function(m) String.Equals(m.Identifier.Identifier.Text, stringLiteral.Token.ValueText, StringComparison.Ordinal))
        End Function

    End Class
End Namespace