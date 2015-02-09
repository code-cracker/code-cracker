Imports System.Runtime.CompilerServices

Module CodeFixTestExtensions
    <Extension>
    Public Function WrapInMethod(code As String, Optional isAsync As Boolean = False) As String
        Return String.Format("
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public {1}{2} Foo(){3}
            {0}
            ' VB Requires value to be used or another analyzer is added which breaks the tests
            Console.WriteLine(a)
        End {2}
    End Class
End Namespace",
                             code,
                             If(isAsync, "Async ", ""),
                             If(isAsync, "Function", "Sub"),
                             If(isAsync, " As Task", ""))
    End Function
End Module
