Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class EmptyCatchBlockAnalyzer
    Inherits CodeCrackerAnalyzerBase

    Public Sub New()
        MyBase.New(ID:="CC0004",
            Title:="Catch block cannot be empty",
            MsgFormat:="{0}",
            Category:=SupportedCategories.Design,
            Description:="An empty catch block suppresses all errors and shouldn't be used.
If the error is expected, consider logging it or changing the control flow such that it is explicit.")
    End Sub

    Public Overrides Sub OnInitialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.CatchBlock)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        Dim catchBlock = DirectCast(context.Node, CatchBlockSyntax)
        If (catchBlock.Statements.Count <> 0) Then Exit Sub
        Dim diag = Diagnostic.Create(GetDescriptor(), catchBlock.GetLocation(), "Empty Catch Block.")
        context.ReportDiagnostic(diag)
    End Sub

End Class
