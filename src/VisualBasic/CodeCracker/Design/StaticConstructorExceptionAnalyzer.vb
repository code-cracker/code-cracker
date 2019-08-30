Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class StaticConstructorExceptionAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.StaticConstructorException.ToDiagnosticId()
        Public Const Title As String = "Don't throw exceptions inside static constructors."
        Public Const MessageFormat As String = "Don't throw exceptions inside static constructors."
        Public Const Category As String = SupportedCategories.Design
    Public Const Description As String = "Static constructors are called before a class is used for the first time. Exceptions thrown
    in static constructors force the use of a try block and should be avoided."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                Id,
                Title,
                MessageFormat,
                Category,
                SeverityConfigurations.CurrentVB(DiagnosticId.StaticConstructorException),
                isEnabledByDefault:=True,
                description:=Description,
                helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.StaticConstructorException))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.SubNewStatement)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
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
