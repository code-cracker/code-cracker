Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class EmptyCatchBlockAnalyzer
    Inherits DiagnosticAnalyzer

    Public Const DiagnosticId = "CC0004"
    Friend Const Title = "Catch block cannot be empty"
    Friend Const MessageFormat = "{0}"
    Friend Const Category = SupportedCategories.Design
    Const Description = "An empty catch block suppresses all errors and shouldn't be used.
If the error is expected, consider logging it or changing the control flow such that it is explicit."

    Friend Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(DiagnosticId,
                                                  Title,
                                                  MessageFormat,
                                                  Category,
                                                  DiagnosticSeverity.Error,
                                                  isEnabledByDefault:=True,
                                                  description:=Description,
                                                  helpLink:=HelpLink.ForDiagnostic(DiagnosticId))

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.CatchBlock)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        Dim catchBlock = DirectCast(context.Node, CatchBlockSyntax)
        If (catchBlock.Statements.Count <> 0) Then Exit Sub
        Dim diag = Diagnostic.Create(Rule, catchBlock.GetLocation(), "Empty Catch Block.")
        context.ReportDiagnostic(diag)
    End Sub
End Class
