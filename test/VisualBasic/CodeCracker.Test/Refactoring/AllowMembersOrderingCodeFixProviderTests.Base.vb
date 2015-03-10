Imports System.Threading
Imports CodeCracker.VisualBasic.Refactoring
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Namespace Refactoring
    Public Class BaseAllowMembersOrderingCodeFixProviderTests
        Inherits CodeFixVerifier(Of AllowMembersOrderingAnalyzer, MockCodeFixProvider)

        <Theory>
        <InlineData("Class", "Property B As String", "Property A As String")>
        Public Async Function BaseAllowMembersOrderingShouldCallIComparerToOrder(typeDeclaration As String, memberA As String, memberB As String) As Task

            Dim codeFixProvider = DirectCast(MyBase.GetCodeFixProvider, MockCodeFixProvider)


            Dim source = String.Format("
Public {0} Foo
    {1}
    {2}
End {0}", typeDeclaration, memberA, memberB)

            Dim expected = String.Format("
Public {0} Foo
    {2}
    {1}
End {0}", typeDeclaration, memberA, memberB)

            Await VerifyBasicFixAsync(source, expected, codeFixProvider:=codeFixProvider)
            Assert.True(codeFixProvider.HasIComparerBeenCalled, "The IComparer must be used to sort the members of that type")
        End Function

        <Theory>
        <InlineData("Class")>
        <InlineData("Structure")>
        Public Async Function BaseAllowMembersOrderingShouldSupportWriteMembers(typeDeclaration As String) As Task
            Dim source = String.Format("
Imports System

Namespace ConsoleApplication1
    {0} Foo
        Public Class Foo2
        End Class
        Public Structure Struct

        End Structure
        Public Shared Operator +(f1 As Foo, f2 As Foo) As Foo
            Return New Foo()
        End Operator
        Sub Method(a As String)

        End Sub
        Public Property Prop As String
        Private Interface IInterface

        End Interface
        Public Enum Eenum
            Enum1
            Enum2 = 1
        End Enum
        Public Custom Event CustomEvent As EventHandler
            AddHandler(value As EventHandler)

            End AddHandler
            RemoveHandler(value As EventHandler)

            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)

            End RaiseEvent
        End Event
        Public Delegate Function doubleDelegate(num As Double) As Double
        Public Sub New()
            Prop = """"
            Field = """"
        End Sub
        Public Event EventField1 As Action
        Public field As String
    End {0}
End Namespace
", typeDeclaration)

            Dim expected = String.Format("
Imports System

Namespace ConsoleApplication1
    {0} Foo
        Private Interface IInterface

        End Interface
        Public Class Foo2
        End Class
        Public Custom Event CustomEvent As EventHandler
            AddHandler(value As EventHandler)

            End AddHandler
            RemoveHandler(value As EventHandler)

            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)

            End RaiseEvent
        End Event
        Public Delegate Function doubleDelegate(num As Double) As Double
        Public Enum Eenum
            Enum1
            Enum2 = 1
        End Enum
        Public Event EventField1 As Action
        Public field As String
        Public Property Prop As String
        Public Shared Operator +(f1 As Foo, f2 As Foo) As Foo
            Return New Foo()
        End Operator
        Public Structure Struct

        End Structure
        Public Sub New()
            Prop = """"
            Field = """"
        End Sub
        Sub Method(a As String)

        End Sub
    End {0}
End Namespace
", typeDeclaration)

            Await VerifyBasicFixAsync(source, expected)
        End Function
        Public Class MockCodeFixProvider
            Inherits BaseAllowMembersOrderingCodeFixProvider
            Public Sub New()
                MyBase.New("Fake codefix")
            End Sub

            Public Property HasIComparerBeenCalled As Boolean

            Protected Overrides Function GetMemberDeclarationComparer(document As Document, cancellationToken As CancellationToken) As IComparer(Of DeclarationStatementSyntax)
                Return New AlphabeticalMemberOrderingComparer(Me)
            End Function
            Friend Class AlphabeticalMemberOrderingComparer
                Implements IComparer(Of DeclarationStatementSyntax)

                ReadOnly parent As MockCodeFixProvider

                Public Sub New(parent As MockCodeFixProvider)
                    Me.parent = parent
                End Sub

                Public Function Compare(x As DeclarationStatementSyntax, y As DeclarationStatementSyntax) As Integer Implements IComparer(Of DeclarationStatementSyntax).Compare
                    parent.HasIComparerBeenCalled = True
                    Return String.Compare(x.ToFullString, y.ToFullString, StringComparison.InvariantCulture)
                End Function
            End Class
        End Class
    End Class
End Namespace