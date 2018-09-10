Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class UnusedParametersTests
        Inherits CodeFixVerifier(Of UnusedParametersAnalyzer, UnusedParametersCodeFixProvider)
        <Fact>
        Public Async Function MethodWithoutParametersDoesNotCreateDiagnostic() As Task
            Const source = "
Class TypeName
    Public Sub Foo()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function UnusedParametersDoesNotCreateDiagnostic() As Task
            Const source = "
Class TypeName
    Public Function Foo(a As Integer) as Integer
        Return a
    End Function
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function MethodWithoutStatementsCreatesDiagnostic() As Task
            Const source = "
Class TypeName
    Public Sub Foo(a As Integer)
    End Sub
End Class"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("a", 3, 20))
        End Function

        <Fact>
        Public Async Function IgnorePartialMethods() As Task
            Const source = "
Partial Class TypeName
    Public Partial Sub Foo(a As Integer)
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function FixUnusedParameter() As Task
            Const source = "
Class TypeName
    Public Sub Foo(a As Integer)
    End Sub
End Class"
            Const fix = "
Class TypeName
    Public Sub Foo()
    End Sub
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function With2ParametersCreatesDiagnostic() As Task
            Const source = "
Class TypeName
    Public Function Foo(a As Integer, b As Integer) As Integer
        Return a
    End Function
End Class"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("b", 3, 39))
        End Function

        <Fact>
        Public Async Function FixUnusedParameterWith2Parameters() As Task
            Const source = "
Class TypeName
    Public Function Foo(a As Integer, b As Integer) As Integer
        Return a
    End Function
End Class"
            Const fix = "
Class TypeName
    Public Function Foo(a As Integer) As Integer
        Return a
    End Function
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function IgnoreOverrides() As Task
            Const source = "
Class Base
    Public Overridable Function Foo(a As Integer) As Integer
        Return a
    End Sub
End Class
Class TypeName
    Inherits Base
    Public Overrides Function Foo(a As Integer) As Integer
        Throw New System.Exception()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function IgnoreMethodsThatImplementAnInterfaceMember() As Task
            Const source = "
Interface IBase
    Function Foo(a As Integer) As Integer
End Interface
Class TypeName
    Implements IBase
    Public Function Foo(a As Integer) As Integer Implements IBase.Foo
        Throw New System.Exception()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function IgnoreMethodsThatMatchEventHandlerPattern() As Task
            Const source = "
Imports System
Class TypeName
    Public Sub Foo(sender As Object, args As EventArgs)
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function IgnoreMethodsThatMatchEventHandlerPatternWithDerivedEventArgs() As Task
            Const source = "
Imports System
Class MyArgs
    Inherits EventArgs
End Class
Class TypeName
    Public Sub Foo(sender As Object, args As MyArgs)
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function DoNotIgnoreMethodsThatMatchEventHandlerPatternButDoesNotReturnVoid() As Task
            Const source = "
Imports System
Class TypeName
    Public Function Foo(sender As Object, args As EventArgs) As Integer
        Throw New Exception()
    End Function
End Class"
            Await VerifyBasicDiagnosticAsync(source,
                                              CreateDiagnosticResult("sender", 4, 25),
                                              CreateDiagnosticResult("args", 4, 43))
        End Function
        <Fact>
        Public Async Function ConstructorWithoutStatementsCreatesDiagnostic() As Task
            Const source = "
Class TypeName
    Public Sub New(a As Integer)
    End Sub
End Class"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("a", 3, 20))
        End Function

        <Fact>
        Public Async Function IgnoreSerializableConstructor() As Task
            Const source = "
Imports System
Imports System.Runtime.Serialization
<Serializable>
Class TypeName
    Implements ISerializable

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub
    Public Overridable Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function DoNotIgnoreSerializableConstructorIfTypeDoesNotImplementISerializable() As Task
            Const source = "
Imports System
Imports System.Runtime.Serialization
<Serializable>
Class TypeName

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub
End Class"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("info", 7, 23), CreateDiagnosticResult("context", 7, 50))
        End Function

        <Fact>
        Public Async Function DoNotIgnoreSerializableConstructorIfTypeDoesNotHaveSerializableAttribute() As Task
            Const source = "
Imports System
Imports System.Runtime.Serialization
Class TypeName
    Implements ISerializable
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub
    Public Overridable Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
    End Sub
End Class"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("info", 6, 23), CreateDiagnosticResult("context", 6, 50))
        End Function

        <Fact>
        Public Async Function FixWhenTheParametersHasReferenceOnSameClass() As Task
            Const source = "
Class TypeName
    Public Sub IsReferencing()
        Foo(1, 2)
    End Sub
    Public Function Foo(a As Integer, b As Integer) as Integer
        Return a
    End Function
End Class"
            Const fix = "
Class TypeName
    Public Sub IsReferencing()
        Foo(1)
    End Sub
    Public Function Foo(a As Integer) as Integer
        Return a
    End Function
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixWhenTheParametersHasReferenceOnDifferentClass() As Task
            Const source = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName().Foo(1, 2)
    End Sub
End Class
Class TypeName
    Public Function Foo(a As Integer, b As Integer) as Integer
        Return a
    End Function
End Class"
            Const fix = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName().Foo(1)
    End Sub
End Class
Class TypeName
    Public Function Foo(a As Integer) as Integer
        Return a
    End Function
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixWhenTheParametersHasReferenceOnDifferentClassOnSharedMethod() As Task
            Const source = "
Class HasRef
    Public Sub IsReferencing()
        TypeName.Foo(1, 2)
    End Sub
End Class
Class TypeName
    Public Shared Function Foo(a As Integer, b As Integer) as Integer
        Return a
    End Function
End Class"
            Const fix = "
Class HasRef
    Public Sub IsReferencing()
        TypeName.Foo(1)
    End Sub
End Class
Class TypeName
    Public Shared Function Foo(a As Integer) as Integer
        Return a
    End Function
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixWhenTheParametersHasReferenceOnConstructor() As Task
            Const source = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName(1, 2)
    End Sub
End Class
Class TypeName
    Public Sub New(a As Integer, b As Integer)
        Dim x = a
    End Sub
End Class"
            Const fix = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName(1)
    End Sub
End Class
Class TypeName
    Public Sub New(a As Integer)
        Dim x = a
    End Sub
End Class"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixParamsInConstructor() As Task
            Const source = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName(1, 2, 3)
    End Sub
End Class
Class TypeName
    Public Sub New(a As Integer, b As Integer, ParamArray c As Integer())
        b = a
    End Sub
End Class"
            Const fix = "
Class HasRef
    Public Sub IsReferencing()
        Dim x = New TypeName(1, 2)
    End Sub
End Class
Class TypeName
    Public Sub New(a As Integer, b As Integer)
        b = a
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixParams() As Task
            Const source = "
Class Foo
    Public Sub IsReferencing()
        Dim x = Bar(1, 2, 3, 4)
    End Sub
    Public Function Bar(a As Integer, b As Integer, ParamArray c As Integer())
        b = a
        return 1
    End Function
End Class"
            Const fix = "
Class Foo
    Public Sub IsReferencing()
        Dim x = Bar(1, 2)
    End Sub
    Public Function Bar(a As Integer, b As Integer)
        b = a
        return 1
    End Function
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixParamsWhenNotInUse() As Task
            Const source = "
Class Foo
    Public Sub IsReferencing()
        Dim x = Bar(1, 2)
    End Sub
    Public Function Bar(a As Integer, b As Integer, ParamArray c As Integer())
        b = a
        return 1
    End Function
End Class"
            Const fix = "
Class Foo
    Public Sub IsReferencing()
        Dim x = Bar(1, 2)
    End Sub
    Public Function Bar(a As Integer, b As Integer)
        b = a
        return 1
    End Function
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function


        <Fact>
        Public Async Function CallToBaseDoesNotCreateDiagnostic() As Task
            Const source = "
Class Base
    Protected Sub New(a As Integer)
        Dim x = a
    End Sub
End Class
Class Derived
    Inherits Base
    Public Sub New(a As Integer)
        MyBase.New(a)
    End Sub
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function CallToBaseWithExpressionDoesNotCreateDiagnostic() As Task
            Const source = "
Class Base
    Protected Sub New(a As Integer)
        dim x = a
    End Sub
End Class
Class Derived
    Inherits Base
    Public Sub New(a As Integer)
        MyBase.New(a + 1)
    End Sub
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function CallWithRefParameterDoesNotCreateDiagnostic() As Task
            Const source = "
Class Base
        Private Function TryParse(input As String, ByRef output As Integer) As Boolean
            output = CInt(input)
            Return True
        End Function
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function CallWithUnusedRefParameterDoesCreateDiagnostic() As Task
            Const source = "
Class Base
        Private Function TryParse(input As String, ByRef output As Integer, ByRef out2 As Integer) As Boolean
            output = CInt(input)
            Return True
        End Function
End Class
"
            Await VerifyBasicDiagnosticAsync(source, CreateDiagnosticResult("out2", 3, 77))
        End Function

        <Fact>
        Public Async Function CallWithDllImport() As Task
            Const source = "
Imports System.Runtime.InteropServices
Class Base
    <DllImport(""x.dll"", CallingConvention:=CallingConvention.Cdecl)> 
    Private Shared Function y(ByRef message As IntPtr) As Integer
    End Function
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function CallWithRefAndEnumerableDoesNotCreateDiagnostic() As Task
            Const source = "
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Class Base
        Private Function TryReplaceTypeMembers(typeBlock As TypeBlockSyntax, membersDeclaration As IEnumerable(Of DeclarationStatementSyntax), sortedMembers As IEnumerable(Of DeclarationStatementSyntax), ByRef orderedType As TypeBlockSyntax) As Boolean
            Dim sortedMembersQueue = New Queue(Of DeclarationStatementSyntax)(sortedMembers)
            Dim orderChanged = False
            orderedType = typeBlock.ReplaceNodes(membersDeclaration,
                                                 Function(original, rewritten)
                                                     Dim newMember = sortedMembersQueue.Dequeue()
                                                     If Not orderChanged And Not original.Equals(newMember) Then
                                                         orderChanged = True
                                                     End If
                                                     Return newMember
                                                 End Function)
            Return orderChanged
        End Function
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function FixAllInSameClass() As Task
            Const source As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1, 2, 3, 4)
    End Sub
    Public Sub Foo(ByVal a As Integer, ByVal b As Integer, ParamArray ByVal c() As Integer)
        a = 1
    End Sub
End Class"
            Const fixtest As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1)
    End Sub
    Public Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        <Fact>
        Public Async Function FixAllInDifferentClass() As Task
            Const source1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1, 2, 3, 4)
    End Sub
End Class"
            Const source2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer, ByVal b As Integer, ParamArray ByVal c() As Integer)
        a = 1
    End Sub
End Class"
            Const fix1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1)
    End Sub
End Class"
            Const fix2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync({source1, source2}, {fix1, fix2})
        End Function

        <Fact>
        Public Async Function FixAllWithOnlyAnOptionalNotPassedInSameClass() As Task
            Const source As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo()
    End Sub
    Public Sub Foo(Optional ByVal b As String = Nothing)
        dim a = 1
    End Sub
End Class"
            Const fixtest As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo()
    End Sub
    Public Sub Foo()
        dim a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        <Fact>
        Public Async Function FixAllWithOptionalNotPassedInSameClass() As Task
            Const source As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1, 2, , 3, 4)
    End Sub
    Public Sub Foo(ByVal a As Integer, ByVal b As Integer, Optional ByVal c As String = Nothing, ParamArray ByVal d() As Integer)
        a = 1
    End Sub
End Class"
            Const fixtest As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1)
    End Sub
    Public Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        <Fact>
        Public Async Function FixAllWithOptionalPassedInSameClass() As Task
            Const source As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1, 2, """", 3, 4)
    End Sub
    Public Sub Foo(ByVal a As Integer, ByVal b As Integer, Optional ByVal c As String = Nothing, ParamArray ByVal d() As Integer)
        a = 1
    End Sub
End Class"
            Const fixtest As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(1)
    End Sub
    Public Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        <Fact>
        Public Async Function FixAllWithOptionalNotPassedInDifferentClass() As Task
            Const source1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1, 2, , 3, 4)
    End Sub
End Class"
            Const source2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer, ByVal b As Integer, Optional ByVal c As String = Nothing, ParamArray ByVal d() As Integer)
        a = 1
    End Sub
End Class"
            Const fix1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1)
    End Sub
End Class"
            Const fix2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync({source1, source2}, {fix1, fix2})
        End Function

        <Fact>
        Public Async Function FixAllWithOptionalPassedInDifferentClass() As Task
            Const source1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1, 2, """", 3, 4)
    End Sub
End Class"
            Const source2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer, ByVal b As Integer, Optional ByVal c As String = Nothing, ParamArray ByVal d() As Integer)
        a = 1
    End Sub
End Class"
            Const fix1 As String = "
Class TypeName
    Public Sub IsReferencing()
        Referenced.Foo(1)
    End Sub
End Class"
            Const fix2 As String = "
Class Referenced
    Public Shared Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync({source1, source2}, {fix1, fix2})
        End Function

        <Fact>
        Public Async Function FixAllWithNamedParametersInSameClass() As Task
            Const source As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(b:= 2, a:= 1)
    End Sub
    Public Sub Foo(ByVal a As Integer, ByVal b As Integer)
        a = 1
    End Sub
End Class"
            Const fixtest As String = "
Class TypeName
    Public Sub IsReferencing()
        Me.Foo(a:= 1)
    End Sub
    Public Sub Foo(ByVal a As Integer)
        a = 1
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        Private Function CreateDiagnosticResult(parameterName As String, line As Integer, column As Integer) As DiagnosticResult
            Return New DiagnosticResult(DiagnosticId.UnusedParameters.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(line, column) _
                .WithMessage(String.Format(UnusedParametersAnalyzer.Message, parameterName))
        End Function
    End Class
End Namespace