Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Public Module VBAnalyzerExtensions
    <Extension> Public Sub RegisterSyntaxNodeAction(Of TLanguageKindEnum As Structure)(context As AnalysisContext, languageVersion As LanguageVersion, action As Action(Of SyntaxNodeAnalysisContext), ParamArray syntaxKinds As TLanguageKindEnum())
        context.RegisterCompilationStartAction(languageVersion, Sub(compilationContext) compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds))
    End Sub

    <Extension> Public Sub RegisterCompilationStartAction(context As AnalysisContext, languageVersion As LanguageVersion, registrationAction As Action(Of CompilationStartAnalysisContext))
        context.RegisterCompilationStartAction(Sub(compilationContext) compilationContext.RunIfVBVersionOrGreater(languageVersion, Sub() registrationAction?.Invoke(compilationContext)))
    End Sub

    <Extension> Private Sub RunIfVBVersionOrGreater(context As CompilationStartAnalysisContext, languageVersion As LanguageVersion, action As Action)
        context.Compilation.RunIfVBVersionOrGreater(action, languageVersion)
    End Sub

    <Extension> Private Sub RunIfVB14OrGreater(context As CompilationStartAnalysisContext, action As Action)
        context.Compilation.RunIfVB14OrGreater(action)
    End Sub

    <Extension> Private Sub RunIfVB14OrGreater(compilation As Compilation, action As Action)
        compilation.RunIfVBVersionOrGreater(action, LanguageVersion.VisualBasic14)
    End Sub

    <Extension> Private Sub RunIfVBVersionOrGreater(compilation As Compilation, action As Action, languageVersion As LanguageVersion)
        Dim vbCompilation = TryCast(compilation, VisualBasicCompilation)
        If vbCompilation Is Nothing Then
            Return
        End If
        vbCompilation.LanguageVersion.RunWithVBVersionOrGreater(action, languageVersion)
    End Sub

    <Extension> Public Sub RunWithVB14OrGreater(languageVersion As LanguageVersion, action As Action)
        languageVersion.RunWithVBVersionOrGreater(action, LanguageVersion.VisualBasic14)
    End Sub

    <Extension> Public Sub RunWithVBVersionOrGreater(languageVersion As LanguageVersion, action As Action, greaterOrEqualThanLanguageVersion As LanguageVersion)
        If languageVersion >= greaterOrEqualThanLanguageVersion Then action?.Invoke()
    End Sub
End Module