Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class StaticConstructorExceptionAnalyzer
        Inherits DiagnosticAnalyzer

        Public Const DiagnosticId As String = "CC0024"
        Public Const Title As String = "Don't throw exception inside static constructors."
        Public Const MessageFormat As String = "Don't throw exceptions inside static constructors."
        Public Const Category As String = SupportedCategories.Design
        Public Const Description As String = "Static constructor are called before the first time a class is used but the caller doesn't control when exactly.
Exception thrown in this context forces callers to use 'try' block around any useage of the class and should be avoided."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                DiagnosticId,
                Title,
                MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault:=True,
                description:=Description,
                helpLink:=HelpLink.ForDiagnostic(DiagnosticId))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.SubNewStatement)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            Dim ctor = DirectCast(context.Node, Syntax.SubNewStatementSyntax)
            If Not ctor.Modifiers.Any(SyntaxKind.SharedKeyword) Then Exit Sub

            Dim constructorBlock = DirectCast(ctor.Parent, Syntax.ConstructorBlockSyntax)
            If Not constructorBlock.Statements.Any() Then Exit Sub

            Dim throwBlock = constructorBlock.ChildNodes.OfType(Of Syntax.ThrowStatementSyntax).FirstOrDefault()
            If throwBlock Is Nothing Then Exit Sub

            context.ReportDiagnostic(Diagnostic.Create(Rule, throwBlock.GetLocation, Title))
        End Sub
    End Class
End Namespace
