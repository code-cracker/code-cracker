Imports Xunit
Imports CodeCracker
Public Class PlaceholderTest
    <Fact>
    Public Sub PleaseRemove()
        Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic12.RunWithVB14OrGreater(Sub()
                                                                                              End Sub)
    End Sub

End Class
