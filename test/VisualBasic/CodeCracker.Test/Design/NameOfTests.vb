Imports CodeCracker.VisualBasic.Design
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Design
    Public Class NameOfTests
        Inherits CodeFixVerifier(Of NameOfAnalyzer, NameOfCodeFixProvider)

        <Theory>
        <InlineData("", "")>
        <InlineData("", "Nothing")>
        <InlineData("", "b")>
        <InlineData("A As String", "b")>
        Public Async Function WhenStringLiteralInMethodShouldNotReportDiagnostic(parameters As String, stringLiteral As String) As Task
            Dim source = $"
Public Class TypeName
  Sub Foo({parameters})
    Dim whatever = """ & stringLiteral & """
  End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhenReferencingExternalSymbolShouldReportDiagnostic() As Task
            Dim source = $"
Imports System
Public Class TypeName
    Sub Foo()
        Dim a = ""Action""
    End Sub
End Class"
            Dim expected = CreateNameofDiagnosticResult("Actio" + "n", 5, 17, DiagnosticId.NameOf_External)
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Theory>
        <InlineData("b As String", "b", "b")>
        <InlineData("[for] As String", "[for]", "[for]")>
        Public Async Function WhenStringLiteralInMethodShouldReportDiagnostic(parameters As String, stringLiteral As String, nameofArgument As String) As Task
            Dim source = $"
Public Class TypeName
  Sub({parameters})
    Dim whatever = """ & stringLiteral & """
  End Sub
End Class"
            Dim expected = CreateNameofDiagnosticResult(nameofArgument, 4, 20)
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function IgnoreIfStringLiteralIsWhitespace() As Task
            Const test = "
Public Class TypeName
    Sub Foo()
        Dim whatever = """"
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoreIfStringLiteralIsNothing() As Task
            Const test = "
Public Class TypeName
    Sub Foo()
        Dim whatever = Nothing
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoreIfConstructorHasNoParameters() As Task
            Const test = "
Public Class TypeName
    Public Sub New()
        dim whatever = ""b""
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenUsingSomeStringInAttributeShouldNotReportDiagnostic() As Task
            Const test = "
public class TypeName
    <Whatever(""a"")>
    <Whatever(""xyz"")>
    Sub Foo(a As String)
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData("xyz", False)>
        <InlineData("NestedClass", True)>
        <InlineData("SomeStruct", True)>
        <InlineData("SomeEnum", True)>
        <InlineData("IInterface", True)>
        <InlineData("N2", True)>
        <InlineData("SomeDelegate", True)>
        <InlineData("readonlyField", True)>
        <InlineData("ParticularEvent", True)>
        <InlineData("Prop", True)>
        <InlineData("TheTypeName", True)>
        <InlineData("Invoke", True)>
        <InlineData("N1", True)>
        <InlineData("N3", True)>
        Public Async Function WhenUsingProgramElementNameStringInMethodInvocation(stringLiteral As String, shouldReportDiatnostic As Boolean) As Task
            Dim source = $"Namespace N1.N2
    Namespace N3
        Public Class TheTypeName
            Private ReadOnly readonlyField As Integer
            Public Property Prop As Integer
            Public Event ParticularEvent As EventHandler
            Public Delegate Sub SomeDelegate(c As Integer, d As Double)

            Public Interface IInterface

            End Interface
            Public Structure SomeStruct

            End Structure
            Public Enum SomeEnum
                defaultMember
            End Enum
            Public Class NestedClass

            End Class
            Public WriteOnly Property Prop1 As Integer
                Set(value As Integer)
                    Invoke(""abc"", """ & stringLiteral & """)
                End Set
            End Property
            Private Sub Invoke(arg1 As String, arg2 As String)

            End Sub
        End Class
    End Namespace
End Namespace"
            If Not shouldReportDiatnostic Then
                Await VerifyBasicHasNoDiagnosticsAsync(source)
            Else
                Dim expected = CreateNameofDiagnosticResult(stringLiteral, 23, 35)
                Await VerifyBasicDiagnosticAsync(source, expected)
            End If
        End Function

        <Fact>
        Public Async Function WhenUsingProgramElementStringInVariableAssignment() As Task
            Const source = "
Public Class TypeName
    Private ReadOnly readonlyField As Integer
    Public Class NestedClass
    End Class

    Public WriteOnly Property Prop As Integer
        Set
            Dim variable = ""NestedClass""
            Dim variable = ""xyz""
            variable = ""readonlyField""
        End Set
    End Property
End Class"

            Dim expectedForFirstAssignment = CreateNameofDiagnosticResult("NestedClass", 9, 28)
            Dim expectedForSecondAssignment = CreateNameofDiagnosticResult("readonlyField", 11, 24)

            Await VerifyBasicDiagnosticAsync(source, expectedForFirstAssignment, expectedForSecondAssignment)
        End Function

        <Fact>
        Public Async Function WhenUsingProgramElementNameStringInAttributeShouldReportDiagnostic() As Task
            Const source = "
Namespace N1.N2
    Public Class TheTypeName
        Private ReadOnly readonlyField As Integer
        Public Property Prop As Integer
        Public Event ParticularEvent As EventHandler
        Public Delegate Function SomeDelegate(c As Integer, d As Double) As Integer
        <Whatever(""N2"")>
        <Whatever(""SomeDelegate"")>
        <Whatever(""readonlyField"")>
        <Whatever(""ParticularEvent"")>
        <Whatever(""Prop"")>
        <Whatever(""TheTypeName"")>
        <Whatever(""Foo"")>
        <Whatever(""N1"")>
        Sub Foo(a As String)
        End Sub
    End Class
End Namespace"

            Dim expected = {
                CreateNameofDiagnosticResult("N2", 8, 19),
                CreateNameofDiagnosticResult("SomeDelegate", 9, 19),
                CreateNameofDiagnosticResult("readonlyField", 10, 19),
                CreateNameofDiagnosticResult("ParticularEvent", 11, 19),
                CreateNameofDiagnosticResult("Prop", 12, 19),
                CreateNameofDiagnosticResult("TheTypeName", 13, 19),
                CreateNameofDiagnosticResult("Foo", 14, 19),
                CreateNameofDiagnosticResult("N1", 15, 19)
            }

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralInObjectInitializer() As Task
            Const source = "
Namespace N1.N2
    Public Class OtherTypeName
        Public Property Prop As String
        Private Property Prop2 As String
        Public Property Prop3 As String
    End Class
    Public Class TypeName
        Sub Foo(a As String)
            Dim instance As New OtherTypeName
            {
                Prop = ""xyz""
                Prop2 = ""OtherTypeName""
                Prop3 = ""Prop2""
            }
        End Sub
    End Class
End Namespace"
            Await VerifyBasicDiagnosticAsync(source, CreateNameofDiagnosticResult("OtherTypeName", 13, 25))
        End Function

        <Fact>
        Public Async Function WhenUsingProgramElementNameInArrayInitializer() As Task
            Const source = "
Public Class TypeName
    Private ReadOnly readonlyField As Integer
    Public Interface IInterface
    End Interface

    Sub Foo(a As String)
        Dim t = {""readonlyField"", ""xyz"", ""IInterface""}
    End Sub
End Class"

            Dim expected =
                {
                    CreateNameofDiagnosticResult("readonlyField", 8, 18),
                    CreateNameofDiagnosticResult("IInterface", 8, 42)
                }
            Await VerifyBasicDiagnosticAsync(source, expected)

        End Function

        <Fact>
        Public Async Function WhenCreatingNewObjectWithStringLiterals() As Task
            Const source = "
Public Structure TheTypeName
    Sub Foo(a As String)
        Dim instance = New OtherTypeName(""b"", ""xyz"")
        instance2 = New OtherTypeName(""TheTypeName"", ""a"")
    End Sub
End Structure"

            Dim expected =
            {
                CreateNameofDiagnosticResult("TheTypeName", 5, 39),
                CreateNameofDiagnosticResult("a", 5, 54)
            }

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralInVariousPlaces() As Task
            Const source = "
Namespace N1.N2
    Namespace N3
        Public Delegate Function SomeDelegate(a As Integer, b As Integer) As Integer

        Public Class BaseTypeName
            Public Sub New(a As String)

            End Sub
        End Class

        Public Class TheTypeName
            Inherits BaseTypeName

            Private ReadOnly readonlyField As Integer
            Public Event ParticularEvent As EventHandler
            Public className As String = ""TheTypeName""
            Public fieldName As String = ""field""
            Public someName As String = ""variable""
            Public namespaceName As String = ""N3""
            Public namespaceName2 As String = ""N2""

            Public Sub Foo()
                String.Format(""{0}"", ""xyz"")
            End Sub
            Public Sub Foo2()
                String.Format(""{0}"", ""readonlyField"")
            End Sub

            Public Sub New()
                MyBase.New(""SomeDelegate"")
            End Sub

            Sub Foo3(a As String)
                Dim dict As New Dictionary(Of String, String) With
                    {
                        {""b"", ""readonlyField""},
                        {""xyz"", ""ParticularEvent""}
                    }
            End Sub

            Public ReadOnly Property Prop As Integer
                Get
                    Dim variable = 5
                    Return variable
                End Get
            End Property

            Public namespaceName3 As String = ""N1.N2""
            Public verbatimString As String = ""
verbatim
string
lines""

        End Class
    End Namespace
End Namespace
"
            Dim expected =
                {
                    CreateNameofDiagnosticResult("TheTypeName", 17, 42),
                    CreateNameofDiagnosticResult("N3", 20, 46),
                    CreateNameofDiagnosticResult("N2", 21, 47),
                    CreateNameofDiagnosticResult("readonlyField", 27, 38),
                    CreateNameofDiagnosticResult("SomeDelegate", 31, 28),
                    CreateNameofDiagnosticResult("readonlyField", 37, 31),
                    CreateNameofDiagnosticResult("ParticularEvent", 38, 33)
                }
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function FixWithVerbatimIdentifiers() As Task
            Const source = "
Public Class TypeName
    Public Sub New(obj As Object)
        Dim name = ""obj""
    End Sub
    Sub Foo(a As String, [for] As Integer, b As String, [integer] As Object)
        Dim whatever = ""[for]""
        Dim whatever1 = ""b""
        Dim whatever2 = ""a""
        Dim whatever3 = ""[integer]""
    End Sub
End Class"
            Const fixTest = "
Public Class TypeName
    Public Sub New(obj As Object)
        Dim name = NameOf(obj)
    End Sub
    Sub Foo(a As String, [for] As Integer, b As String, [integer] As Object)
        Dim whatever = NameOf([for])
        Dim whatever1 = NameOf(b)
        Dim whatever2 = NameOf(a)
        Dim whatever3 = NameOf([integer])
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fixTest)
        End Function

        <Fact>
        Public Async Function IgnoreIfMethodHasNoParameters() As Task
            Const test = "
Public Class TypeName
    Sub Foo()
        Dim whatever = """"
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoreIfMethodHasParametersUnlineOfStringLiteral() As Task
            Const test = "
Public Class TypeName
    Sub Foo(a As String)
        Dim whatever = ""b""
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralEqualsParameterNameReturnAnalyzerCreatesDiagnostic() As Task
            Const test = "
Public Class TypeName
    Sub Foo(b As String)
        Dim whatever = ""b""
    End Sub
End Class"
            Dim expected = CreateNameofDiagnosticResult("b", 4, 24)
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameof() As Task
            Const test = "
Public Class TypeName
    Sub New(b As String)
        Dim whatever = ""b""
    End Sub
End Class"

            Const fixtest = "
Public Class TypeName
    Sub New(b As String)
        Dim whatever = NameOf(b)
    End Sub
End Class"

            Await VerifyBasicFixAsync(test, fixtest, 0)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameofMustKeepComments() As Task
            Const test = "
Public Class TypeName
    Sub New(b As String)
        'a
        Dim whatever = ""b"" 'd
        'b
    End Sub
End Class"

            Const fixtest = "
Public Class TypeName
    Sub New(b As String)
        'a
        Dim whatever = NameOf(b) 'd
        'b
    End Sub
End Class"

            Await VerifyBasicFixAsync(test, fixtest, 0)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralEqualsParameterNameInMethodFixItToNameof() As Task
            Const test = "
Public Class TypeName
    Sub Foo(b As String)
        Dim whatever = ""b""
    End Sub
End Class"

            Const fixtest = "
Public Class TypeName
    Sub Foo(b As String)
        Dim whatever = NameOf(b)
    End Sub
End Class"

            Await VerifyBasicFixAsync(test, fixtest, 0)
        End Function

        <Fact>
        Public Async Function WhenUsingStringLiteralEqualsParameterNameInMethodMustKeepComments() As Task
            Const test = "
Public Class TypeName
    Sub Foo(b As String)
        'a
        Dim whatever = ""b""'d
        'b
    End Sub
End Class"

            Const fixtest = "
Public Class TypeName
    Sub Foo(b As String)
        'a
        Dim whatever = NameOf(b) 'd
        'b
    End Sub
End Class"

            Await VerifyBasicFixAsync(test, fixtest, 0)
        End Function

        <Fact>
        Public Async Function IgnoreAttributes() As Task
            Const test = "
Public Class TypeName
    <Whatever(""a"")>
    Sub Foo(a as String)
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function FixAll() As Task
            Const source = "
Public Class TypeName
    Sub Go(x As Integer, y As Integer, z As Integer)
        Dim a = ""x""
        Dim b = ""y""
        Dim c = ""z""
    End Sub
End Class"
            Const fixtest = "
Public Class TypeName
    Sub Go(x As Integer, y As Integer, z As Integer)
        Dim a = NameOf(x)
        Dim b = NameOf(y)
        Dim c = NameOf(z)
    End Sub
End Class"
            Await VerifyBasicFixAllAsync(source, fixtest)
        End Function

        <Theory>
        <InlineData("NestedClass")>
        <InlineData("SomeStruct")>
        <InlineData("SomeEnum")>
        <InlineData("IInterface")>
        <InlineData("N2")>
        <InlineData("SomeDelegate")>
        <InlineData("readonlyField")>
        <InlineData("ParticularEvent")>
        <InlineData("Prop")>
        <InlineData("TheTypeName")>
        <InlineData("Invoke")>
        <InlineData("N1")>
        <InlineData("N3")>
        Public Async Function WhenUsingProgramElementNameStringInMethodInvocationThenFixUpdatesAsExpected(stringLiteral As String) As Task
            Const source = "
Namespace N1.N2
    Namespace N3
        Public Class TheTypeName

            Private ReadOnly readonlyField As Integer
            Public Property Prop As Integer
            Public Event ParticularEvent As EventHandler
            Public Enum SomeEnum
                SomeValue
            End Enum
            Public Delegate Function SomeDelegate(c As Integer, d As Double) As Integer
            Public Interface IInterface

            End Interface
            Public Structure SomeStruct

            End Structure
            Public Class NestedClass

            End Class
            Public WriteOnly Property Prop2 As Integer
                Set(value As Integer)
                    Invoke(""abc"", ~REPLACE~)
                End Set
            End Property
            Private Sub Invoke(arg1 As String, arg2 As String)

            End Sub
        End Class
    End Namespace
End Namespace
"
            Await VerifyBasicFixAllAsync(source.Replace("~REPLACE~", $"""{stringLiteral}"""), source.Replace("~REPLACE~", $"NameOf({stringLiteral})"))
        End Function

        <Fact>
        Public Async Function IgnoreNamesNotDeclaredYet() As Task
            Const test = "
Public Class TypeName
    Sub Foo()
        Dim str = ""x""
        Dim x = 1
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        Private Function CreateNameofDiagnosticResult(nameofArgument As String, diagnosticLine As Integer, diagnosticColumn As Integer, Optional id As DiagnosticId = DiagnosticId.NameOf) As DiagnosticResult
            Return New DiagnosticResult(id.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(diagnosticLine, diagnosticColumn) _
                .WithMessage($"Use 'NameOf({nameofArgument})' instead of specifying the program element name.")
        End Function
    End Class
End Namespace