Namespace Usage.MethodAnalyzers
    Public Class MethodInformation
        Public Property MethodName As String
        Public Property MethodFullDefinition As String
        Public Property ArgumentIndex As Integer
        Public Property MethodAction As Action(Of List(Of Object))

        Public Sub New(methodName As String, methodFullDefinition As String, methodAction As Action(Of List(Of Object)), Optional argumentIndex As Integer = 0)
            Me.MethodName = methodName
            Me.MethodFullDefinition = methodFullDefinition
            Me.ArgumentIndex = argumentIndex
            Me.MethodAction = methodAction
        End Sub
    End Class
End Namespace
