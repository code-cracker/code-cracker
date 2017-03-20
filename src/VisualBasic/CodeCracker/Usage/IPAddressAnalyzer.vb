Imports System.Collections.Immutable
Imports System.Reflection
Imports CodeCracker.VisualBasic.Usage.MethodAnalyzers
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class IPAddressAnalyzer
    Inherits DiagnosticAnalyzer

    Friend Const Title = "Your IP Address syntax is incorrect."
    Friend Const MessageFormat = "{0}"
    Private Const Description = "An error was found parsing the IP Address string."

    Friend Shared Rule As New DiagnosticDescriptor(
        DiagnosticId.IPAddress.ToDiagnosticId(),
        Title,
        MessageFormat,
        SupportedCategories.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault:=True,
        description:=Description,
        helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.IPAddress))

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.InvocationExpression)
    End Sub

    Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
        If (context.IsGenerated()) Then Return
        Dim method As New MethodInformation("Parse",
                                            "Public Shared Overloads Function Parse(ipString As String) As System.Net.IPAddress",
                                            Sub(args) parseMethodInfo.Value.Invoke(Nothing, {args(0).ToString()}))
        Dim checker = New methodchecker(context, Rule)
        checker.AnalyzeMethod(method)
    End Sub

    Private Shared ReadOnly objectType As New Lazy(Of Type)(Function() System.Type.GetType("System.Net.IPAddress, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
    Private Shared ReadOnly parseMethodInfo As New Lazy(Of MethodInfo)(Function() objectType.Value.GetRuntimeMethod("Parse", {GetType(String)}))
End Class
