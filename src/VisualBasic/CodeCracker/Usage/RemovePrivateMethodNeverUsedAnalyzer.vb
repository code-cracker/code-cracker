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
		Private Const Description = "When a private method is declared but not used, remove it to avoid confusion."

		Friend Shared Rule As New DiagnosticDescriptor(
			DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId(),
			Title,
			Message,
			SupportedCategories.Usage,
			DiagnosticSeverity.Info,
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
			If Not methodStatement.Modifiers.Any(Function(a) a.ValueText = SyntaxFactory.Token(SyntaxKind.PrivateKeyword).ValueText) Then Exit Sub
			If IsMethodUsed(methodStatement, context.SemanticModel) Then Exit Sub
			Dim diag = Diagnostic.Create(Rule, methodStatement.GetLocation())
			context.ReportDiagnostic(diag)
		End Sub

		Private Function IsMethodUsed(methodTarget As MethodStatementSyntax, semanticModel As SemanticModel) As Boolean
			Dim typeDeclaration = TryCast(methodTarget.Parent.Parent, ClassBlockSyntax)
			If typeDeclaration Is Nothing Then Return True

			Dim classStatement = typeDeclaration.ClassStatement
			If classStatement Is Nothing Then Return True

			If Not classStatement.Modifiers.Any(SyntaxKind.PartialKeyword) Then
				Return IsMethodUsed(methodTarget, typeDeclaration)
			End If

			Dim symbol = semanticModel.GetDeclaredSymbol(typeDeclaration)

			Return symbol Is Nothing Or symbol.DeclaringSyntaxReferences.Any(Function(r) IsMethodUsed(methodTarget, r.GetSyntax().Parent))
		End Function

		Private Function IsMethodUsed(methodTarget As MethodStatementSyntax, typeDeclaration As SyntaxNode) As Boolean
			Dim hasIdentifier = typeDeclaration?.DescendantNodes()?.OfType(Of IdentifierNameSyntax)()
			If (hasIdentifier Is Nothing Or Not hasIdentifier.Any()) Then Return False
			Return hasIdentifier.Any(Function(a) a IsNot Nothing And a.Identifier.ValueText.Equals(methodTarget?.Identifier.ValueText))
		End Function

	End Class

	Partial Public Class Foo

	End Class
End Namespace
