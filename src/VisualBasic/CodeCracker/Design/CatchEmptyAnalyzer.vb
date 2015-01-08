Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class CatchEmptyAnalyzer
    Inherits DiagnosticAnalyzer

    Public Const DiagnosticId = "CC0003"
    Friend Const Title = "Your catch may includes some Exception"
    Friend Const MessageFormat = "{0}"
    Friend Const Category = SupportedCategories.Design
    Friend Shared Rule As New DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLink:=HelpLink.ForDiagnostic(DiagnosticId))

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.CatchStatement)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        Dim catchStatement = DirectCast(context.Node, Microsoft.CodeAnalysis.VisualBasic.Syntax.CatchStatementSyntax)
        If catchStatement Is Nothing Then Exit Sub

        If catchStatement.IdentifierName Is Nothing Then
            Dim diag = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Consider including an Exception Class in catch.")
            context.ReportDiagnostic(diag)
        End If
    End Sub
End Class
