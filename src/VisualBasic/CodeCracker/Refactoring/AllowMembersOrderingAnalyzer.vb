Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Refactoring
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class AllowMembersOrderingAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.AllowMembersOrdering.ToDiagnosticId()

        Friend Const Title = "Ordering member inside this type."
        Friend Const MessageFormat = "Ordering member inside this type."
        Friend Const Category = SupportedCategories.Refactoring
        Friend Shared Rule As New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            SeverityConfigurations.CurrentVB(DiagnosticId.AllowMembersOrdering),
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.AllowMembersOrdering))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyze, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock)
        End Sub

        Public Sub Analyze(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim typeSyntax = TryCast(context.Node, TypeBlockSyntax)
            If typeSyntax Is Nothing Then Exit Sub

            Dim currentChildNodesOrder = typeSyntax.Members()
            If currentChildNodesOrder.Count > 1 Then ' If there is only member, we don't need to worry about the order
                context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation()))
            End If
        End Sub
    End Class
End Namespace