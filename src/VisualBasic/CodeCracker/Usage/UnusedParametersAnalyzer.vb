Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class UnusedParametersAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Unused parameters."
        Friend Const Message = "Parameter '{0}' is not used."
        Private Const Description = "When a method declares a parameter and does not use it might bring incorrect concolusions for anyone reading the code and anso demands the parameter when the method is called unnecessarily.
You should delete the parameter in such cases."

        Friend Shared Rule As New DiagnosticDescriptor(
        DiagnosticId.UnusedParameters.ToDiagnosticId(),
        Title,
        Message,
        SupportedCategories.Usage,
        DiagnosticSeverity.Warning,
        True,
        Description,
        HelpLink.ForDiagnostic(DiagnosticId.UnusedParameters),
        WellKnownDiagnosticTags.Unnecessary)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer, SyntaxKind.SubBlock, SyntaxKind.ConstructorBlock, SyntaxKind.FunctionBlock)
        End Sub

        Private Sub Analyzer(context As SyntaxNodeAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim methodOrConstructor = TryCast(context.Node, MethodBlockBaseSyntax)
            If methodOrConstructor Is Nothing Then Exit Sub
            Dim model = context.SemanticModel
            If Not IsCandidateForRemoval(methodOrConstructor, model) Then Exit Sub
            Dim parameters = methodOrConstructor.BlockStatement.ParameterList.Parameters.ToDictionary(Function(p) p, Function(p) model.GetDeclaredSymbol(p))
            Dim ctor = TryCast(methodOrConstructor, ConstructorBlockSyntax)
            ' TODO: Check if used in MyBase
            If methodOrConstructor.Statements.Any() Then
                Dim dataFlowAnalysis = model.AnalyzeDataFlow(methodOrConstructor.Statements.First, methodOrConstructor.Statements.Last)
                If Not dataFlowAnalysis.Succeeded Then Exit Sub
                For Each parameter In parameters
                    Dim parameterSymbol = parameter.Value
                    If parameterSymbol Is Nothing Then Continue For
                    If Not dataFlowAnalysis.ReadInside.Contains(parameterSymbol) AndAlso
                        Not dataFlowAnalysis.WrittenInside.Contains(parameterSymbol) Then
                        context = CreateDiagnostic(context, parameter.Key)
                    End If
                Next
            Else
                For Each parameter In parameters.Keys
                    context = CreateDiagnostic(context, parameter)
                Next
            End If
        End Sub

        Private Shared Function IsCandidateForRemoval(methodOrConstructor As MethodBlockBaseSyntax, semanticModel As SemanticModel) As Boolean
            If methodOrConstructor.BlockStatement.Modifiers.Any(Function(m) m.ValueText = "Partial" OrElse m.ValueText = "Overrides") OrElse
            Not methodOrConstructor.BlockStatement.ParameterList?.Parameters.Any() Then Return False

            Dim method = TryCast(methodOrConstructor, MethodBlockSyntax)
            If method IsNot Nothing Then
                If method.SubOrFunctionStatement.ImplementsClause IsNot Nothing Then Return False
                Dim methodSymbol = semanticModel.GetDeclaredSymbol(method)
                If methodSymbol Is Nothing Then Return False
                Dim typeSymbol = methodSymbol.ContainingType
                If typeSymbol.Interfaces.SelectMany(Function(i) i.GetMembers()).
                Any(Function(member) methodSymbol.Equals(typeSymbol.FindImplementationForInterfaceMember(member))) Then Return False

                If IsEventHandlerLike(method, semanticModel) Then Return False
            Else
                Dim constructor = TryCast(methodOrConstructor, ConstructorBlockSyntax)
                If constructor IsNot Nothing Then
                    If IsSerializationConstructor(constructor, semanticModel) Then Return False
                Else
                    Return False
                End If
            End If
            Return True
        End Function

        Private Shared Function IsSerializationConstructor(constructor As ConstructorBlockSyntax, model As SemanticModel) As Boolean
            If constructor.SubNewStatement.ParameterList.Parameters.Count <> 2 Then Return False
            Dim constructorSymbol = model.GetDeclaredSymbol(constructor)
            Dim typeSymbol = constructorSymbol?.ContainingType
            If If(Not typeSymbol?.Interfaces.Any(Function(i) i.ToString() = "System.Runtime.Serialization.ISerializable"), True) Then Return False
            If Not typeSymbol.GetAttributes().Any(Function(a) a.AttributeClass.ToString() = "System.SerializableAttribute") Then Return False
            Dim serializationInfoType = TryCast(model.GetTypeInfo(constructor.SubNewStatement.ParameterList.Parameters(0).AsClause.Type).Type, INamedTypeSymbol)
            If serializationInfoType Is Nothing Then Return False
            If Not serializationInfoType.AllBaseTypesAndSelf().Any(Function(type) type.ToString() = "System.Runtime.Serialization.SerializationInfo") Then Return False

            Dim streamContextType = TryCast(model.GetTypeInfo(constructor.SubNewStatement.ParameterList.Parameters(1).AsClause.Type).Type, INamedTypeSymbol)
            If streamContextType Is Nothing Then Return False
            Return streamContextType.AllBaseTypesAndSelf().Any(Function(type) type.ToString() = "System.Runtime.Serialization.StreamingContext")
        End Function

        Private Shared Function IsEventHandlerLike(method As MethodBlockSyntax, model As SemanticModel) As Boolean

            If method.SubOrFunctionStatement.ParameterList.Parameters.Count <> 2 OrElse
            method.IsKind(SyntaxKind.FunctionBlock) Then Return False

            Dim senderType = model.GetTypeInfo(method.SubOrFunctionStatement.ParameterList.Parameters(0).AsClause.Type).Type
            If senderType.SpecialType <> SpecialType.System_Object Then Return False
            Dim eventArgsType = TryCast(model.GetTypeInfo(method.SubOrFunctionStatement.ParameterList.Parameters(1).AsClause.Type).Type, INamedTypeSymbol)
            If eventArgsType Is Nothing Then Return False
            Return eventArgsType.AllBaseTypesAndSelf().Any(Function(type) type.ToString() = "System.EventArgs")
        End Function

        Private Function CreateDiagnostic(context As SyntaxNodeAnalysisContext, parameter As ParameterSyntax) As SyntaxNodeAnalysisContext
            Dim propsDic = New Dictionary(Of String, String)
            propsDic.Add("identifier", parameter.Identifier.Identifier.Text)
            Dim props = propsDic.ToImmutableDictionary()
            Dim diag = Diagnostic.Create(Rule, parameter.GetLocation(), props, parameter.Identifier.Identifier.ValueText)
            context.ReportDiagnostic(diag)
            Return context
        End Function
    End Class
End Namespace