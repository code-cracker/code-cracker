Imports CodeCracker.VisualBasic.Refactoring
Imports Xunit

Namespace Refactoring
    Public Class StyleCopAllowMembersOrderingCodeFixProviderTests
        Inherits CodeFixTest(Of AllowMembersOrderingAnalyzer, StyleCopAllowMembersOrderingCodeFixProvider)

        <Fact>
        Public Async Function StyleCopAllowMembersOrderingShouldAssureMembersOrderingByType() As Task
            Const source = "
Class Foo
    Public Class Class1

    End Class
    Public Structure Struct

    End Structure
    Sub Method()

    End Sub
    Public Property Prop As Integer
    Public Interface IInterface

    End Interface
    Public Enum Enum1
        EnumVal
    End Enum
    Public Custom Event Event1 As System.Action
        AddHandler(value As System.Action)

        End AddHandler
        RemoveHandler(value As System.Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Public Delegate Function Delegate1() As Double
    Sub New()

    End Sub
    Public Event EventField As System.Action
    Public b = 0
End Class"

            Const expected = "
Class Foo
    Public b = 0
    Sub New()

    End Sub
    Public Delegate Function Delegate1() As Double
    Public Event EventField As System.Action
    Public Custom Event Event1 As System.Action
        AddHandler(value As System.Action)

        End AddHandler
        RemoveHandler(value As System.Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Public Enum Enum1
        EnumVal
    End Enum
    Public Interface IInterface

    End Interface
    Public Property Prop As Integer
    Sub Method()

    End Sub
    Public Structure Struct

    End Structure
    Public Class Class1

    End Class
End Class"

            Await VerifyBasicFixAsync(source, expected)
        End Function

        <Fact>
        Public Async Function StyleCopAllowMembersOrderingShouldAssureMembersOrderByModifiers() As Task
            Const source = "
Public Class Foo1
    Private p = 0
    Protected q = 0
    Protected Friend r = 0
    Friend s = 0
    Public t = 0
    Shared u = 0
    Public Shared v = 0
    Const x = 0
    Public Const z = 0
End Class"

            Const expected = "
Public Class Foo1
    Public Const z = 0
    Public Shared v = 0
    Public t = 0
    Friend s = 0
    Protected Friend r = 0
    Protected q = 0
    Const x = 0
    Shared u = 0
    Private p = 0
End Class"

            Await VerifyBasicFixAsync(source, expected)
        End Function

        <Fact>
        Public Async Function StyleCopAllowmembersOrderingShouldAssureMembersOrderByAlphabeticalOrder() As Task
            Const source = "
Public Class Foo2
    Private c = 2, d = 3
    Private a = 0, b = 1
    Event EventField1 As System.Action
    Event EventField As System.Action
    Delegate Function Delegate2() As Double
    Delegate Function Delegate1() As Double
    Custom Event Event2 As System.Action
        AddHandler(value As System.Action)

        End AddHandler
        RemoveHandler(value As System.Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Custom Event Event1 As System.Action
        AddHandler(value As Action)

        End AddHandler
        RemoveHandler(value As Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Enum Enum2
        Enum1
    End Enum
    Enum Enum1
        Enum1
    End Enum
    Interface Interface2

    End Interface
    Interface Interface1

    End Interface
    Property Property2 As Integer
    Property Property1 As Integer
    Public Shared Operator +(A As Foo2, B As Foo2)
        Return 0
    End Operator
    Public Shared Operator -(a As Foo2, b As Foo2)
        Return 0
    End Operator
    Sub Method2()

    End Sub
    Sub Method1()

    End Sub
    Public Structure Sruct1

    End Structure
    Public Structure Sruct

    End Structure
    Public Class Class2

    End Class
    Public Class Class1
    End Class
End Class"

            Const expected = "
Public Class Foo2
    Private a = 0, b = 1
    Private c = 2, d = 3
    Delegate Function Delegate1() As Double
    Delegate Function Delegate2() As Double
    Event EventField As System.Action
    Event EventField1 As System.Action
    Custom Event Event1 As System.Action
        AddHandler(value As Action)

        End AddHandler
        RemoveHandler(value As Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Custom Event Event2 As System.Action
        AddHandler(value As System.Action)

        End AddHandler
        RemoveHandler(value As System.Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
    Enum Enum1
        Enum1
    End Enum
    Enum Enum2
        Enum1
    End Enum
    Interface Interface1

    End Interface
    Interface Interface2

    End Interface
    Property Property1 As Integer
    Property Property2 As Integer
    Public Shared Operator -(a As Foo2, b As Foo2)
        Return 0
    End Operator
    Public Shared Operator +(A As Foo2, B As Foo2)
        Return 0
    End Operator
    Sub Method1()

    End Sub
    Sub Method2()

    End Sub
    Public Structure Sruct

    End Structure
    Public Structure Sruct1

    End Structure
    Public Class Class1
    End Class
    Public Class Class2

    End Class
End Class"

            Await VerifyBasicFixAsync(source, expected)
        End Function

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Async Function StyleCopAllowMembersOrderingShouldAssureMembersOrderByStyleCopPatterns(typeDeclaration As String) As Task
            Dim source = "
Imports System
Namespace ConsoleApplication1
    " & typeDeclaration & " Foo3
        Public Class Foo4
        End Class
        Public Structure Struct

        End Structure
        Public Shared Operator +(f1 As Foo3, f2 As Foo3)
            Return New Foo3
        End Operator
        Public Shared Operator -(f1 As Foo3, f2 As Foo3)
            Return New Foo3
        End Operator
        Sub Method(a As String)

        End Sub
        Friend Sub Method(a As Integer)

        End Sub
        Public Sub Method1()

        End Sub
        Public Sub Method()

        End Sub
        Public Property Prop As String
        Public Interface IInterface

        End Interface
        Public Enum Enum1
            Enum1
            Enum2
        End Enum
        Public Custom Event Event1 As Action
            AddHandler(value As Action)

            End AddHandler
            RemoveHandler(value As Action)

            End RemoveHandler
            RaiseEvent()

            End RaiseEvent
        End Event
        Public Delegate Function Delegate1(num As Double) As Double
        Public Sub New()
            Prop = """"
            Field1 = """"
            Field = """"
        End Sub
        Public Event EventField1 As Action
        Public Event EventField As Action
        Public Field As String
        Public Shared Field1 As String
    End " & typeDeclaration & "

End Namespace"

            Dim expected = "
Imports System
Namespace ConsoleApplication1
    " & typeDeclaration & " Foo3
        Public Shared Field1 As String
        Public Field As String
        Public Sub New()
            Prop = """"
            Field1 = """"
            Field = """"
        End Sub
        Public Delegate Function Delegate1(num As Double) As Double
        Public Event EventField As Action
        Public Event EventField1 As Action
        Public Custom Event Event1 As Action
            AddHandler(value As Action)

            End AddHandler
            RemoveHandler(value As Action)

            End RemoveHandler
            RaiseEvent()

            End RaiseEvent
        End Event
        Public Enum Enum1
            Enum1
            Enum2
        End Enum
        Public Interface IInterface

        End Interface
        Public Property Prop As String
        Public Shared Operator -(f1 As Foo3, f2 As Foo3)
            Return New Foo3
        End Operator
        Public Shared Operator +(f1 As Foo3, f2 As Foo3)
            Return New Foo3
        End Operator
        Public Sub Method()

        End Sub
        Public Sub Method1()

        End Sub
        Friend Sub Method(a As Integer)

        End Sub
        Sub Method(a As String)

        End Sub
        Public Structure Struct

        End Structure
        Public Class Foo4
        End Class
    End " & typeDeclaration & "

End Namespace"

            Await VerifyBasicFixAsync(source, expected)
        End Function
    End Class
End Namespace