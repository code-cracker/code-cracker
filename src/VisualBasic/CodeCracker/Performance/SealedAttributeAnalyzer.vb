Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Collections.Immutable
Imports CodeCracker.Extensions

Namespace Performance
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class SealedAttributeAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.SealedAttribute.ToDiagnosticId()
        Public Const Title As String = "Unsealed Attribute"
        Public Const MessageFormat As String = "Mark '{0}' as NotInheritable."
        Public Const Category As String = SupportedCategories.Performance
        Public Const Description As String = "Framework methods that retrieve attributes by default search the entire inheritence hierarchy of the attribute class. Marking the type as NotInheritable eliminates this search and can improve performance."
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLink:=HelpLink.ForDiagnostic(Id))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSymbolAction(AddressOf Analyze, SymbolKind.NamedType)
        End Sub

        Private Sub Analyze(context As SymbolAnalysisContext)
            Dim type = DirectCast(context.Symbol, INamedTypeSymbol)
            If type.TypeKind <> TypeKind.Class Then Exit Sub

            If Not IsAttribute(type) Then Exit Sub
            If (type.IsAbstract OrElse type.IsSealed) Then Exit Sub
            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations(0), type.Name))
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
End Namespace