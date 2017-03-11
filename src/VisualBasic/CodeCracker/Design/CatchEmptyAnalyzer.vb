Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports System.Collections.Immutable

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class CatchEmptyAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.CatchEmpty.ToDiagnosticId()
        Public Const Title As String = "Your catch should include an Exception"
        Public Const MessageFormat As String = "{0}"
        Public Const Category As String = SupportedCategories.Design
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.CatchEmpty))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.CatchStatement)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim catchStatement = DirectCast(context.Node, Syntax.CatchStatementSyntax)
                If catchStatement Is Nothing Then Exit Sub

            If catchStatement.IdentifierName Is Nothing Then
                Dim diag = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Consider adding an Exception to the catch.")
                context.ReportDiagnostic(diag)
            End If
        End Sub

    End Class
End Namespace