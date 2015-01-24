Imports System.Runtime.CompilerServices

Module CodeFixTestExtensions
    <Extension>
    Public Function WrapInMethod(code As String) As String
        Return "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            code
            ' VB Requires value to be used or another analyzer is added which breaks the tests
            Console.WriteLine(a)
        End Sub
    End Class
End Namespace".Replace("code", code)
    End Function
End Module
