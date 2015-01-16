Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public MustInherit Class CodeCrackerAnalyzerBase
    Inherits DiagnosticAnalyzer

    Public ReadOnly Property DiagnosticID As String = ""
    Public ReadOnly Property Title As String = ""
    Public ReadOnly Property Category As String = ""
    Public ReadOnly Property Severity As DiagnosticSeverity = DiagnosticSeverity.Warning
    Public ReadOnly Property MsgFormat As String = ""
    Public ReadOnly Property IsEnabled As Boolean = True
    Public ReadOnly Property Description As String = ""

    Protected _Diagnostics As Immutable.ImmutableArray(Of DiagnosticDescriptor)

    Friend Sub New(ID As String, Title As String, MsgFormat As String, Category As String, Optional Description As String = "", Optional IsEnable As Boolean = True, Optional Severity As DiagnosticSeverity = DiagnosticSeverity.Warning)
        MyBase.New()
        Me._DiagnosticID = ID
        Me._Title = "ForInArrayAnalyzer"
        Me._Category = Category
        Me._Description = Description
        Me._MsgFormat = MsgFormat
        Me._Severity = Severity
        Me._IsEnabled = IsEnabled
        Me._Diagnostics = Immutable.ImmutableArray.Create(GetDescriptor())
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As Immutable.ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return _Diagnostics
        End Get
    End Property
    Friend Function GetDescriptor() As DiagnosticDescriptor
        Return New DiagnosticDescriptor(DiagnosticID, Title, MsgFormat, Category, Severity, IsEnabled, Description, HelpLink.ForDiagnostic(DiagnosticID))
    End Function
    Public Overrides Sub Initialize(context As AnalysisContext)
        OnInitialize(context)
    End Sub

    Public MustOverride Sub OnInitialize(context As AnalysisContext)

End Class
