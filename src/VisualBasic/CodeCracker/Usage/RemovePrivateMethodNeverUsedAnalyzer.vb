Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class RemovePrivateMethodNeverUsedAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Unused Method"
        Friend Const Message = "Method is not used."
        Private Const Description = "Unused private methods can be safely removed as they are unnecessary."

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId(),
            Title,
            Message,
            SupportedCategories.Usage,
            SeverityConfigurations.CurrentVB(DiagnosticId.RemovePrivateMethodNeverUsed),
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.RemovePrivateMethodNeverUsed))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement)
        End Sub

        Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim methodStatement = DirectCast(context.Node, MethodStatementSyntax)
            If methodStatement.HandlesClause IsNot Nothing Then Exit Sub
            If Not methodStatement.Modifiers.Any(Function(a) a.ValueText = SyntaxFactory.Token(SyntaxKind.PrivateKeyword).ValueText) Then Exit Sub
            If (IsMethodAttributeAnException(methodStatement)) Then Return
            If IsMethodUsed(methodStatement, context.SemanticModel) Then Exit Sub
            Dim props = New Dictionary(Of String, String) From {{"identifier", methodStatement.Identifier.Text}}.ToImmutableDictionary()
            Dim diag = Diagnostic.Create(Rule, methodStatement.GetLocation(), props)
            context.ReportDiagnostic(diag)
        End Sub

        Private Function IsMethodAttributeAnException(methodStatement As MethodStatementSyntax) As Boolean
            For Each attributeList In methodStatement.AttributeLists
                For Each attribute In attributeList.Attributes
                    Dim identifierName = TryCast(attribute.Name, IdentifierNameSyntax)
                    Dim nameText As String = Nothing
                    If (identifierName IsNot Nothing) Then
                        nameText = identifierName?.Identifier.Text
                    Else
                        Dim qualifiedName = TryCast(attribute.Name, QualifiedNameSyntax)
                        If (qualifiedName IsNot Nothing) Then
                            nameText = qualifiedName.Right?.Identifier.Text
                        End If
                    End If
                    If (nameText Is Nothing) Then Continue For
                    If (IsExcludedAttributeName(nameText)) Then Return True
                Next
            Next
            Return False
        End Function

        Private Shared ReadOnly excludedAttributeNames As String() = {"Fact", "ContractInvariantMethod", "DataMember"}

        Private Shared Function IsExcludedAttributeName(attributeName As String) As Boolean
            Return excludedAttributeNames.Contains(attributeName)
        End Function

        Private Function IsMethodUsed(methodTarget As MethodStatementSyntax, semanticModel As SemanticModel) As Boolean
            Dim typeDeclaration = TryCast(methodTarget.Parent.Parent, ClassBlockSyntax)
            If typeDeclaration Is Nothing Then Return True

            Dim classStatement = typeDeclaration.ClassStatement
            If classStatement Is Nothing Then Return True

            If Not classStatement.Modifiers.Any(SyntaxKind.PartialKeyword) Then
                Return IsMethodUsed(methodTarget, typeDeclaration)
            End If

            Dim symbol = semanticModel.GetDeclaredSymbol(typeDeclaration)

            Return symbol Is Nothing OrElse symbol.DeclaringSyntaxReferences.Any(Function(r) IsMethodUsed(methodTarget, r.GetSyntax().Parent))
        End Function

        Private Function IsMethodUsed(methodTarget As MethodStatementSyntax, typeDeclaration As SyntaxNode) As Boolean
            Dim hasIdentifier = typeDeclaration?.DescendantNodes()?.OfType(Of IdentifierNameSyntax)()
            If (hasIdentifier Is Nothing OrElse Not hasIdentifier.Any()) Then Return False
            Return hasIdentifier.Any(Function(a) a IsNot Nothing AndAlso a.Identifier.ValueText.Equals(methodTarget?.Identifier.ValueText))
        End Function

    End Class
End Namespace
