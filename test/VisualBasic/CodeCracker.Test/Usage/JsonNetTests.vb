Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Xunit

Namespace Usage
    Public Class JsonNetTests
        Inherits CodeFixVerifier

        Private Const TestCode = "
Imports System
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Namespace ConsoleApplication1
    Class Person
        Public Sub New()
            {0}
        End Sub
    End Class
End Namespace"

        Private Shared Function CreateDiagnosticResult(line As Integer, column As Integer) As DiagnosticResult
            Return New DiagnosticResult With {
                .Id = DiagnosticId.JsonNet.ToDiagnosticId(),
                .Message = "Error parsing boolean value. Path '', line 0, position 0.",
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                .Locations = {New DiagnosticResultLocation("Test0.vb", line, column)}
            }
        End Function

        Protected Overrides Function GetDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New JsonNetAnalyzer
        End Function

        <Fact>
        Public Async Function IfDeserializeObjectIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Newtonsoft.Json.JsonConvert.DeserializeObject(of Person)(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(8, 70))
        End Function

        <Fact>
        Public Async Function IfAbbreviatedDeserializeObjectIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "JsonConvert.DeserializeObject(of Person)(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(8, 54))
        End Function

        <Fact>
        Public Async Function IfDeserializeObjetIdentifierFoundAndJsonTextIsCorrectDoesNotCreateDiagnostic() As Task
            Dim test = String.Format(TestCode, "Newtonsoft.Json.JsonConvert.DeserializeObject(of Person)(""{""""name"""":""""foo""""}"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfAbbreviatedDeserializeObjetIdentifierFoundAndJsonTextIsCorrectDoesNotCreateDiagnostic() As Task
            Dim test = String.Format(TestCode, "JsonConvert.DeserializeObject(of Person)(""{""""name"""":""""foo""""}"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfJObjectParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Newtonsoft.Json.Linq.JObject.Parse(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(8, 48))
        End Function

        <Fact>
        Public Async Function IfAbbreviatedJObjectParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "JObject.Parse(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(8, 27))
        End Function

        <Fact>
        Public Async Function IfJObjectParseFoundAndJsonTextIsCorrectDoesNotCreateDiagnostic() As Task
            Dim test = String.Format(TestCode, "JObject.Parse(""{""""name"""":""""foo""""}"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfJArrayParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Newtonsoft.Json.Linq.Jarray.Parse(""foo"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfAbbreviatedJArrayParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Jarray.Parse(""foo"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfJArrayParseFoundAndJsonTextIsCorrectDoesNotCreateDiagnostic() As Task
            Dim test = String.Format(TestCode, "JArray.Parse(""{""""name"""":""""foo""""}"")")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

    End Class
End Namespace