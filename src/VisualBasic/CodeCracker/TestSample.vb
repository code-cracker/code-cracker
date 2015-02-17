Option Strict On
Option Infer On
Option Explicit On
Imports System

Namespace ConsoleApplication1
    Class TypeName
        Implements System.IDisposable
        Private field As D = D.Create()
        Private field2 As D()

        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Function Create() As D
            Return New D()
        End Function
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace