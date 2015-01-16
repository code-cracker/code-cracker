Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class SealedAttributeAnalyzer
    Inherits CodeCrackerAnalyzerBase

    Public Sub New()
        MyBase.New(PerformanceDiagnostics.SealedAttributeId,
                   Title:="Unsealed Attribute",
                   MsgFormat:="Mark '{0}' as MustInherit.",
                   Category:=Performance,
                   Description:="Framework methods that retrieve attributes by default search the entire inheritence hierarchy of the attribute class. Marking the type as sealed eliminates this search and can improve performance.")
    End Sub

    Public Overrides Sub OnInitialize(context As AnalysisContext)
        context.RegisterSymbolAction(AddressOf Analyze, SymbolKind.NamedType)
    End Sub

    Private Sub Analyze(context As SymbolAnalysisContext)
        Dim type = DirectCast(context.Symbol, INamedTypeSymbol)
        If type.TypeKind <> TypeKind.Class Then Exit Sub

        If Not IsAttribute(type) Then Exit Sub
        If (type.IsAbstract OrElse type.IsSealed) Then Exit Sub
        context.ReportDiagnostic(Diagnostic.Create(GetDescriptor, type.Locations(0), type.Name))
    End Sub

    Public Shared Function IsAttribute(symbol As ITypeSymbol) As Boolean
        Dim base = symbol.BaseType
        Dim attributeName = GetType(Attribute).Name
        While base IsNot Nothing
            If base.Name = attributeName Then Return True
            base = base.BaseType
        End While
        Return False
    End Function

End Class
