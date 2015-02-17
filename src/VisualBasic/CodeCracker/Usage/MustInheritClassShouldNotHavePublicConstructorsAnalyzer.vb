Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class MustInheritClassShouldNotHavePublicConstructorsAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "MustInherit class should not have public constructors."
        Friend Const MessageFormat = "Constructor should not be public."

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            helpLink:=HelpLink.ForDiagnostic(DiagnosticId.AbstractClassShouldNotHavePublicCtors))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SubNewStatement)
        End Sub
        Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim constructor = DirectCast(context.Node, SubNewStatementSyntax)
            If Not constructor.Modifiers.Any(Function(m) m.IsKind(SyntaxKind.PublicKeyword)) Then Exit Sub

            Dim classDeclaration = constructor.FirstAncestorOfType(Of ClassBlockSyntax)
            If classDeclaration Is Nothing Then Exit Sub
            If Not classDeclaration.Begin.Modifiers.Any(Function(m) m.IsKind(SyntaxKind.MustInheritKeyword)) Then Exit Sub

            Dim diag = Diagnostic.Create(Rule, constructor.GetLocation())
            context.ReportDiagnostic(diag)
        End Sub
    End Class
End Namespace

