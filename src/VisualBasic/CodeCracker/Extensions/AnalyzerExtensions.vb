Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Public Module AnalyzerExtensions
    <Extension> Public Sub RegisterSyntaxNodeAction(Of TLanguageKindEnum As Structure)(context As AnalysisContext, languageVersion As LanguageVersion, action As Action(Of SyntaxNodeAnalysisContext), ParamArray syntaxKinds As TLanguageKindEnum())
        context.RegisterCompilationStartAction(languageVersion, Sub(compilationContext) compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds))
    End Sub

    <Extension> Public Sub RegisterCompilationStartAction(context As AnalysisContext, languageVersion As LanguageVersion, registrationAction As Action(Of CompilationStartAnalysisContext))
        context.RegisterCompilationStartAction(Sub(compilationContext) compilationContext.RunIfCSharp6OrGreater(Sub() registrationAction?.Invoke(compilationContext)))
    End Sub

    <Extension> Private Sub RunIfCSharp6OrGreater(context As CompilationStartAnalysisContext, action As Action)
        context.Compilation.RunIfCSharp6OrGreater(action)
    End Sub

    <Extension> Private Sub RunIfCSharp6OrGreater(compilation As Compilation, action As Action)
        Dim vbCompilation = TryCast(compilation, VisualBasicCompilation)
        If vbCompilation Is Nothing Then
            Return
        End If
        vbCompilation.LanguageVersion.RunIfCSharp6OrGreater(action)
    End Sub

    <Extension> Private Sub RunIfCSharp6OrGreater(languageVersion As LanguageVersion, action As Action)
        If languageVersion >= LanguageVersion.VisualBasic14 Then action?.Invoke()
    End Sub

    <Extension> Public Function WithSameTriviaAs(target As SyntaxNode, source As SyntaxNode) As SyntaxNode
        If target Is Nothing Then
            Throw New ArgumentNullException("target")
        End If
        If source Is Nothing Then
            Throw New ArgumentNullException("source")
        End If

        Return target.WithLeadingTrivia(source.GetLeadingTrivia()).WithTrailingTrivia(source.GetTrailingTrivia())
    End Function
End Module