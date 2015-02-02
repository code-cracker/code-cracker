Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class ParameterRefactoryAnalyzer
    Inherits DiagnosticAnalyzer

    Friend Const Title = "You should use a class."
    Friend Const MessageFormat = "When the method has more than three parameters, use a class."
    Friend Const Category = SupportedCategories.Refactoring

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        Throw New NotImplementedException()
    End Sub
End Class
