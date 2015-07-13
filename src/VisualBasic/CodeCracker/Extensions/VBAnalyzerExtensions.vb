Imports System
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Module VBAnalyzerExtensions
    <Extension> Public Sub RegisterSyntaxNodeAction(Of TLanguageKindEnum As Structure)(context As AnalysisContext, languageVersion As LanguageVersion, action As Action(Of SyntaxNodeAnalysisContext), ParamArray syntaxKinds As TLanguageKindEnum())
        context.RegisterCompilationStartAction(languageVersion, Sub(compilationContext) compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds))
    End Sub

    <Extension> Public Sub RegisterCompilationStartAction(context As AnalysisContext, languageVersion As LanguageVersion, registrationAction As Action(Of CompilationStartAnalysisContext))
        context.RegisterCompilationStartAction(Sub(compilationContext) compilationContext.RunIfVBVersionOrGreater(languageVersion, Sub() registrationAction?.Invoke(compilationContext)))
    End Sub

    <Extension> Private Sub RunIfVBVersionOrGreater(context As CompilationStartAnalysisContext, languageVersion As LanguageVersion, action As Action)
        context.Compilation.RunIfVBVersionOrGreater(action, languageVersion)
        If False Then context.RegisterCodeBlockAction(Nothing) 'to go around RS1012
    End Sub

    <Extension> Private Sub RunIfVB14OrGreater(context As CompilationStartAnalysisContext, action As Action)
        context.Compilation.RunIfVB14OrGreater(action)
        If False Then context.RegisterCodeBlockAction(Nothing) 'to go around RS1012
    End Sub

    <Extension> Private Sub RunIfVB14OrGreater(compilation As Compilation, action As Action)
        compilation.RunIfVBVersionOrGreater(action, LanguageVersion.VisualBasic14)
    End Sub

    <Extension> Private Sub RunIfVBVersionOrGreater(compilation As Compilation, action As Action, languageVersion As LanguageVersion)
        Dim vbCompilation = TryCast(compilation, VisualBasicCompilation)
        If vbCompilation Is Nothing Then
            Return
        End If
        vbCompilation.LanguageVersion.RunWithVBVersionOrGreater(action, languageVersion)
    End Sub

    <Extension> Public Sub RunWithVB14OrGreater(languageVersion As LanguageVersion, action As Action)
        languageVersion.RunWithVBVersionOrGreater(action, languageVersion.VisualBasic14)
    End Sub

    <Extension> Public Sub RunWithVBVersionOrGreater(languageVersion As LanguageVersion, action As Action, greaterOrEqualThanLanguageVersion As LanguageVersion)
        If languageVersion >= greaterOrEqualThanLanguageVersion Then action?.Invoke()
    End Sub

    <Extension> Public Function HasAttributeOnAncestorOrSelf(node As SyntaxNode, attributeName As String) As Boolean
        Dim vbNode = TryCast(node, VisualBasicSyntaxNode)
        If (vbNode Is Nothing) Then Throw New System.Exception("Node is not a VB node.")
        Return vbNode.HasAttributeOnAncestorOrSelf(attributeName)
    End Function

    <Extension> Public Function HasAttributeOnAncestorOrSelf(node As SyntaxNode, ParamArray attributeNames As String()) As Boolean
        Dim vbNode = TryCast(node, VisualBasicSyntaxNode)
        If (vbNode Is Nothing) Then Throw New System.Exception("Node is not a VB node.")
        For Each attributeName In attributeNames
            If (vbNode.HasAttributeOnAncestorOrSelf(attributeName)) Then Return True
        Next
        Return False
    End Function

    <Extension> Public Function HasAttributeOnAncestorOrSelf(node As VisualBasicSyntaxNode, attributeName As String) As Boolean
        Dim parentMethod = DirectCast(node.FirstAncestorOrSelfOfType(GetType(MethodBlockSyntax), GetType(ConstructorBlockSyntax)), MethodBlockBaseSyntax)
        If If(parentMethod?.BlockStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim type = DirectCast(node.FirstAncestorOrSelfOfType(GetType(ClassBlockSyntax), GetType(StructureBlockSyntax)), TypeBlockSyntax)
        While (type IsNot Nothing)
            If type.BlockStatement.AttributeLists.HasAttribute(attributeName) Then Return True
            type = DirectCast(type.FirstAncestorOfType(GetType(ClassBlockSyntax), GetType(StructureBlockSyntax)), TypeBlockSyntax)
        End While
        Dim propertyBlock = node.FirstAncestorOrSelfOfType(Of PropertyBlockSyntax)()
        If If(propertyBlock?.PropertyStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim accessor = node.FirstAncestorOrSelfOfType(Of AccessorBlockSyntax)()
        If If(accessor?.AccessorStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim anInterface = node.FirstAncestorOrSelfOfType(Of InterfaceBlockSyntax)()
        If If(anInterface?.InterfaceStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim anEnum = node.FirstAncestorOrSelfOfType(Of EnumBlockSyntax)()
        If If(anEnum?.EnumStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim theModule = node.FirstAncestorOrSelfOfType(Of ModuleBlockSyntax)()
        If If(theModule?.ModuleStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim eventBlock = node.FirstAncestorOrSelfOfType(Of EventBlockSyntax)()
        If If(eventBlock?.EventStatement.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim theEvent = TryCast(node, EventStatementSyntax)
        If If(theEvent?.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim theProperty = TryCast(node, PropertyStatementSyntax)
        If If(theProperty?.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim field = TryCast(node, FieldDeclarationSyntax)
        If If(field?.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim parameter = TryCast(node, ParameterSyntax)
        If If(parameter?.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Dim aDelegate = TryCast(node, DelegateStatementSyntax)
        If If(aDelegate?.AttributeLists.HasAttribute(attributeName), False) Then
            Return True
        End If
        Return False
    End Function

    <Extension> Public Function HasAttribute(attributeLists As SyntaxList(Of AttributeListSyntax), attributeName As String) As Boolean
        Return attributeLists.SelectMany(Function(a) a.Attributes).Any(Function(a) a.Name.ToString().EndsWith(attributeName, StringComparison.OrdinalIgnoreCase))
    End Function

End Module