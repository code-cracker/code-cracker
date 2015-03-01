Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Style
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class InterfaceNameAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "You should add letter 'I' before interface name."
        Friend Const MessageFormat = "Consider naming interfaces starting with 'I'"
        Friend Const Category = SupportedCategories.Style
        Private Const Description = "Consider naming interfaces starting with 'I'."

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.InterfaceName.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.InterfaceName)
        )

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeClass, SyntaxKind.InterfaceStatement)
        End Sub

        Private Shared Sub AnalyzeClass(context As SyntaxNodeAnalysisContext)
            Dim interfaceDeclaration = DirectCast(context.Node, InterfaceStatementSyntax)
            Dim model = context.SemanticModel
            Dim name = interfaceDeclaration.Identifier.Text
            If (name.StartsWith("I", StringComparison.OrdinalIgnoreCase)) Then Exit Sub
            Dim errorMsg = String.Format(MessageFormat, MessageFormat)
            Dim diag = Diagnostic.Create(Rule, interfaceDeclaration.GetLocation(), errorMsg)
            context.ReportDiagnostic(diag)
        End Sub
    End Class
End Namespace