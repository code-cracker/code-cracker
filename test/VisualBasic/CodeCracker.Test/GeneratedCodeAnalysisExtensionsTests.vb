Imports FluentAssertions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Collections.Immutable
Imports Xunit
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports CodeCracker.VisualBasic

Public Class GeneratedCodeAnalysisExtensionsTests
    Private Const baseProjectPath = "D:\ClassLibrary11\"

    <Theory>
    <InlineData(baseProjectPath + "A.g.VB")>
    <InlineData(baseProjectPath + "A.g.vb")>
    <InlineData(baseProjectPath + "B.g.vb")>
    <InlineData(baseProjectPath + "A.g.i.vb")>
    <InlineData(baseProjectPath + "B.g.i.vb")>
    <InlineData(baseProjectPath + "A.designer.vb")>
    <InlineData(baseProjectPath + "A.generated.vb")>
    <InlineData(baseProjectPath + "B.generated.vb")>
    <InlineData(baseProjectPath + "AssemblyInfo.vb")>
    <InlineData(baseProjectPath + "A.AssemblyAttributes.vb")>
    <InlineData(baseProjectPath + "B.AssemblyAttributes.vb")>
    <InlineData(baseProjectPath + "AssemblyAttributes.vb")>
    <InlineData(baseProjectPath + "Service.vb")>
    <InlineData(baseProjectPath + "TemporaryGeneratedFile_.vb")>
    <InlineData(baseProjectPath + "TemporaryGeneratedFile_A.vb")>
    <InlineData(baseProjectPath + "TemporaryGeneratedFile_B.vb")>
    Public Sub IsOnGeneratedFile(fileName As String)
        fileName.IsOnGeneratedFile().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(baseProjectPath + "TheAssemblyInfo.vb")>
    <InlineData(baseProjectPath + "A.vb")>
    <InlineData(baseProjectPath + "TheTemporaryGeneratedFile_A.vb")>
    <InlineData(baseProjectPath + "TheService.vb")>
    <InlineData(baseProjectPath + "TheAssemblyAttributes.vb")>
    Public Sub IsNotOnGeneratedFile(fileName As String)
        fileName.IsOnGeneratedFile().Should().BeFalse()
    End Sub

    <Fact>
    Public Sub IsContextOnGeneratedFile()
        Dim context As SyntaxNodeAnalysisContext = GetContext(
"Class TypeName
End Class", baseProjectPath + "TemporaryGeneratedFile_.vb")
        context.IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData("<System.Diagnostics.DebuggerNonUserCode> Class TypeName
    End Class")>
    <InlineData("<System.Diagnostics.DebuggerNonUserCodeAttribute> Class TypeName
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAClass(source As String)
        GetContext(Of ClassBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData("<System.Diagnostics.DebuggerNonUserCode> Module TypeName
    End Module")>
    <InlineData("<System.Diagnostics.DebuggerNonUserCodeAttribute> Module TypeName
    End Module")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAModule(source As String)
        GetContext(Of ModuleBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCode> Sub Foo()
        End Sub
    End Class")>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCodeAttribute> Sub Foo()
        End Sub
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAMethod(source As String)
        GetContext(Of MethodBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCode> Sub Foo()
            System.Console.WriteLine(1)
        End Sub
    End Class")>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCodeAttribute> Sub Foo()
            System.Console.WriteLine(1)
        End Sub
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod(source As String)
        GetContext(Of InvocationExpressionSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCode> Property Foo As Integer
    End Class")>
    <InlineData(
    "Class TypeName
        <System.Diagnostics.DebuggerNonUserCodeAttribute> Property Foo As Integer
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAProperty(source As String)
        GetContext(Of PropertyStatementSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        <System.Diagnostics.DebuggerNonUserCode>
        Public ReadOnly Property Foo() As Integer
            Get
                Return _foo
            End Get
        End Property
    End Class")>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        <System.Diagnostics.DebuggerNonUserCodeAttribute>
        Public ReadOnly Property Foo() As Integer
            Get
                Return _foo
            End Get
        End Property
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAPropertyCheckingTheGet(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        Public ReadOnly Property Foo() As Integer
            <System.Diagnostics.DebuggerNonUserCode>
            Get
                Return _foo
            End Get
        End Property
    End Class")>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        Public ReadOnly Property Foo() As Integer
            <System.Diagnostics.DebuggerNonUserCodeAttribute>
            Get
                Return _foo
            End Get
        End Property
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAPropertyGet(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        Public WriteOnly Property Foo() As Integer
            <System.Diagnostics.DebuggerNonUserCode>
            Set(ByVal value As Integer)
                _foo = value
            End Set
        End Property
    End Class")>
    <InlineData(
    "Class TypeName
        Private _foo As Integer
        Public WriteOnly Property Foo() As Integer
            <System.Diagnostics.DebuggerNonUserCodeAttribute>
            Set(ByVal value As Integer)
                _foo = value
            End Set
        End Property
    End Class")>
    Public Sub HasDebuggerNonUserCodeAttributeOnAPropertySet(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData("<System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> Class TypeName
    End Class")>
    <InlineData("<System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> Class TypeName
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnAClass(source As String)
        GetContext(Of ClassBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData("<System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> Interface ITypeName
    End Interface")>
    <InlineData("<System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> Interface ITypeName
    End Interface")>
    Public Sub HasGeneratedCodeAttributeOnAnInterface(source As String)
        GetContext(Of InterfaceBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "<System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
    Enum A
        a
    End Enum")>
    <InlineData(
    "<System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
    Enum A
        a
    End Enum")>
    Public Sub HasGeneratedCodeAttributeOnEnum(source As String)
        GetContext(Of EnumBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> private i as Integer
    End Class")>
    <InlineData(
    "Class TypeName
        <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> private i as Integer
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnAField(source As String)
        GetContext(Of FieldDeclarationSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
    <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> Event a As Action
    End Class")>
    <InlineData(
    "Class TypeName
    <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> Event a As Action
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnAnEvent(source As String)
        GetContext(Of EventStatementSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        public Sub Foo(<System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> i as Integer)
        End Sub
    End Class")>
    <InlineData(
    "Class TypeName
        public Sub Foo(<System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> i as Integer)
        End Sub
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnParameter(source As String)
        GetContext(Of ParameterSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> Delegate Sub A()
    End Class")>
    <InlineData(
    "Class TypeName
        <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> Delegate Sub A()
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnDelegate(source As String)
        GetContext(Of DelegateStatementSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "<System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)> Class TypeName
        Structure Nested
        End Structure
    End Class")>
    <InlineData(
    "<System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)> Class TypeName
        Structure Nested
        End Structure
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnNestedClass(source As String)
        GetContext(Of StructureBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
        Public Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
        Public Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnCustomEvent(source As String)
        GetContext(Of EventStatementSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
        Public Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
        Public Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnCustomEventInsideAccessor(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnCustomEventAdd(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
        End Event
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnCustomEventRemove(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Theory>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCode(Nothing, Nothing)>
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
        End Event
    End Class")>
    <InlineData(
    "Class TypeName
        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event Click As EventHandler
            <System.CodeDom.Compiler.GeneratedCodeAttribute(Nothing, Nothing)>
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            End RaiseEvent
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
        End Event
    End Class")>
    Public Sub HasGeneratedCodeAttributeOnCustomEventRaise(source As String)
        GetContext(Of AccessorBlockSyntax)(source).IsGenerated().Should().BeTrue()
    End Sub

    <Fact>
    Public Sub WithAutoGeneratedCommentBasedOnWebForms()
        GetContext(Of ClassBlockSyntax)(
"'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Partial Public Class _Default
End Class").IsGenerated().Should().BeTrue()
    End Sub

    <Fact>
    Public Sub WithAutoGeneratedCommentEmpty()
        GetContext(
"'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------").IsGenerated().Should().BeTrue()
    End Sub

    Private Shared Function GetContext(code As String, Optional fileName As String = baseProjectPath + "a.vb") As SyntaxNodeAnalysisContext
        Return GetContext(Of CompilationUnitSyntax)(code, fileName)
    End Function

    Private Shared Function GetContext(Of T As SyntaxNode)(code As String, Optional fileName As String = baseProjectPath + "a.vb") As SyntaxNodeAnalysisContext
        Dim tree = SyntaxFactory.ParseSyntaxTree(code, path:=fileName)
        Dim compilation = VisualBasicCompilation.Create("comp.dll", New SyntaxTree() {tree})
        Dim root = tree.GetRoot()
        Dim semanticModel = compilation.GetSemanticModel(tree)
        Dim analyzerOptions = New AnalyzerOptions(ImmutableArray(Of AdditionalText).Empty)
        Dim node = root.DescendantNodesAndSelf().OfType(Of T)().First()
        Dim context = New SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions,
                                                    Sub(diag)
                                                    End Sub,
                                                    Function(diag)
                                                        Return True
                                                    End Function, Nothing)
        Return context
    End Function
End Class