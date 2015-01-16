Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

'<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class CatchEmptyAnalyzer
    Inherits CodeCrackerAnalyzerBase

    Public Sub New()
        MyBase.New(DesignDiagnostics.CatchEmptyAnalyerId, "Your catch may includes some Exception", "{0}", SupportedCategories.Design)
    End Sub

    Public Overrides Sub OnInitialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.CatchStatement)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        Dim catchStatement = DirectCast(context.Node, Microsoft.CodeAnalysis.VisualBasic.Syntax.CatchStatementSyntax)
        If catchStatement Is Nothing Then Exit Sub

        If catchStatement.IdentifierName Is Nothing Then
            Dim diag = Diagnostic.Create(GetDescriptor(), catchStatement.GetLocation(), "Consider including an Exception Class in catch.")
            context.ReportDiagnostic(diag)
        End If
    End Sub

End Class
