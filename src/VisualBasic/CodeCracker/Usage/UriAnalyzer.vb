Imports CodeCracker.VisualBasic.Usage.MethodAnalyzers
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Collections.Immutable

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class UriAnalyzer
        Inherits DiagnosticAnalyzer
        Friend Const Title As String = "Your Uri syntax is wrong."
        Friend Const MessageFormat As String = "{0}"
        Friend Const Category As String = SupportedCategories.Usage

        Private Const Description As String = "This diagnostic checks the Uri string and triggers if the parsing fail " + "by throwing an exception."

        Friend Shared Rule As New DiagnosticDescriptor(DiagnosticId.Uri.ToDiagnosticId(), Title, MessageFormat, Category, DiagnosticSeverity.[Error], isEnabledByDefault:=True,
            description:=Description, helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.Uri))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.ObjectCreationExpression)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If context.IsGenerated() Then Return
            Dim mainConstrutor = New MethodInformation("Uri",
                                                       "Public Overloads Sub New(uriString As String)",
                                                       Sub(args)
                                                           If args(0) Is Nothing Then Return
                                                           Dim a = New Uri(args(0).ToString())
                                                       End Sub)
            Dim constructorWithUriKind = New MethodInformation("Uri",
                                                               "Public Overloads Sub New(uriString As String, uriKind As System.UriKind)",
                                                               Sub(args)
                                                                   If args(0) Is Nothing Then Return
                                                                   Dim a = New Uri(args(0).ToString(), DirectCast(args(1), UriKind))
                                                               End Sub)
            Dim checker = New MethodChecker(context, Rule)
            checker.AnalyzeConstructor(mainConstrutor)
            checker.AnalyzeConstructor(constructorWithUriKind)
        End Sub
    End Class
End Namespace