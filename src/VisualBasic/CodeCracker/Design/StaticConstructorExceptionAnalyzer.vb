Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class StaticConstructorExceptionAnalyzer
    Inherits CodeCrackerAnalyzerBase

    Public Sub New()
        MyBase.New(ID:="CC0024",
            Title:="Don't throw exception inside static constructors.",
            MsgFormat:="Don't throw exceptions inside static constructors.",
            Category:=SupportedCategories.Design,
            Description:="Static constructor are called before the first time a class is used but the caller doesn't control when exactly.
Exception thrown in this context forces callers to use 'try' block around any useage of the class and should be avoided.")
    End Sub

    Public Overrides Sub OnInitialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.SubNewStatement)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        Dim ctor = DirectCast(context.Node, Syntax.SubNewStatementSyntax)
        If Not ctor.Modifiers.Any(SyntaxKind.SharedKeyword) Then Exit Sub

        Dim constructorBlock = DirectCast(ctor.Parent, Syntax.ConstructorBlockSyntax)
        If Not constructorBlock.Statements.Any() Then Exit Sub

        Dim throwBlock = constructorBlock.ChildNodes.OfType(Of Syntax.ThrowStatementSyntax).FirstOrDefault()
        If throwBlock Is Nothing Then Exit Sub

        context.ReportDiagnostic(Diagnostic.Create(GetDescriptor(), throwBlock.GetLocation, Title))
    End Sub
End Class
