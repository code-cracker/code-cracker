using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Xunit;

namespace CodeCracker.Test.CSharp
{
    public class GeneratedCodeAnalysisExtensionsTests
    {
        [Theory]
        [InlineData("A.g.CS")]
        [InlineData("A.g.cs")]
        [InlineData("B.g.cs")]
        [InlineData("A.g.i.cs")]
        [InlineData("B.g.i.cs")]
        [InlineData("A.designer.cs")]
        [InlineData("A.generated.cs")]
        [InlineData("B.generated.cs")]
        [InlineData("AssemblyInfo.cs")]
        [InlineData("A.AssemblyAttributes.cs")]
        [InlineData("B.AssemblyAttributes.cs")]
        [InlineData("AssemblyAttributes.cs")]
        [InlineData("Service.cs")]
        [InlineData("TemporaryGeneratedFile_.cs")]
        [InlineData("TemporaryGeneratedFile_A.cs")]
        [InlineData("TemporaryGeneratedFile_B.cs")]
        public void IsOnGeneratedFile(string fileName) => fileName.IsOnGeneratedFile().Should().BeTrue();

        [Theory]
        [InlineData("TheAssemblyInfo.cs")]
        [InlineData("A.cs")]
        [InlineData("TheTemporaryGeneratedFile_A.cs")]
        [InlineData("TheService.cs")]
        [InlineData("TheAssemblyAttributes.cs")]
        public void IsNotOnGeneratedFile(string fileName) => fileName.IsOnGeneratedFile().Should().BeFalse();

        [Fact]
        public void SyntaxNodeAnalysis_IsContextOnGeneratedFile() =>
            GetSyntaxNodeAnalysisContext("class TypeName { }", "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_IsContextOnGeneratedFile() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>("class TypeName { }", "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxTreeAnalysis_IsContextOnGeneratedFile() =>
            GetSyntaxTreeAnalysisContext("class TypeName { }", "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAClass() =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>("[System.Diagnostics.DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAClass() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>("[System.Diagnostics.DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasAbbreviatedDebuggerNonUserCodeAttributeOnAClass() =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>("using System.Diagnostics; [DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasAbbreviatedDebuggerNonUserCodeAttributeOnAClass() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>("using System.Diagnostics; [DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAMethod() =>
            GetSyntaxNodeAnalysisContext<MethodDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAMethod() =>
            GetSymbolAnalysisContext<MethodDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod() =>
            GetSyntaxNodeAnalysisContext<InvocationExpressionSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { System.Console.WriteLine(1); } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod() =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { var i = 1; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAProperty() =>
            GetSyntaxNodeAnalysisContext<PropertyDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] int Foo { get; set; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAProperty() =>
            GetSymbolAnalysisContext<PropertyDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] int Foo { get; set; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyCheckGet() =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCode] int Foo { get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyCheckGet() =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCode] int Foo { get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyGet() =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyGet() =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertySet() =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] set { foo = value; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertySet() =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] set { foo = value; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAClass() =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAClass() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnInterface() =>
            GetSyntaxNodeAnalysisContext<InterfaceDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] interface ITypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnInterface() =>
            GetSymbolAnalysisContext<InterfaceDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] interface ITypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnEnum() =>
            GetSyntaxNodeAnalysisContext<EnumDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] enum A { a }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnEnum() =>
            GetSymbolAnalysisContext<EnumDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] enum A { a }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAField() =>
            GetSyntaxNodeAnalysisContext<FieldDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] private int i; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAField() =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] private int i; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventField() =>
            GetSyntaxNodeAnalysisContext<EventFieldDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event System.Action a; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventField() =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event System.Action a; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclaration() =>
            GetSyntaxNodeAnalysisContext<EventDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclaration() =>
            GetSymbolAnalysisContext<EventDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationCheckingAccessor() =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationCheckingAccessor() =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationAccessor() =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCode(null, null)] add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationAccessor() =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCode(null, null)] add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnParameter() =>
            GetSyntaxNodeAnalysisContext<ParameterSyntax>("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCode(null, null)] int i) { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnParameter() =>
            GetSymbolAnalysisContext<ParameterSyntax>("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCode(null, null)] int i) { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnDelegate() =>
            GetSyntaxNodeAnalysisContext<DelegateDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] delegate void A(); }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnDelegate() =>
            GetSymbolAnalysisContext<DelegateDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] delegate void A(); }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnReturnValue() =>
            GetSyntaxNodeAnalysisContext<ReturnStatementSyntax>("class TypeName {[return: System.CodeDom.Compiler.GeneratedCode(null, null)] int Foo() { return 1; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnNestedClass() =>
            GetSyntaxNodeAnalysisContext<StructDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { struct Nested { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_HasGeneratedCodeAttributeOnNestedClass() =>
            GetSymbolAnalysisContext<StructDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { struct Nested { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_WithAutoGeneratedCommentBasedOnWebForms() =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication3
{
    public partial class _Default
    {
    }
}").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxTreeAnalysis_WithAutoGeneratedCommentBasedOnWebForms() =>
            GetSyntaxTreeAnalysisContext(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication3
{
    public partial class _Default
    {
    }
}").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxTreeAnalysis_WithAutoGeneratedCommentEmpty() =>
            GetSyntaxTreeAnalysisContext(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------").IsGenerated().Should().BeTrue();

        [Fact]
        public void SymbolicAnalysis_WithAutoGeneratedCommentBasedOnWebForms() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication3
{
    public partial class _Default
    {
    }
}").IsGenerated().Should().BeTrue();

        [Fact]
        public void SyntaxNodeAnalysis_WithAutoGeneratedCommentEmpty() =>
            GetSyntaxNodeAnalysisContext(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------").IsGenerated().Should().BeTrue();

        private static SyntaxNodeAnalysisContext GetSyntaxNodeAnalysisContext(string code, string fileName = "a.cs") => GetSyntaxNodeAnalysisContext<CompilationUnitSyntax>(code, fileName);

        private static SyntaxNodeAnalysisContext GetSyntaxNodeAnalysisContext<T>(string code, string fileName = "a.cs") where T : SyntaxNode
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var compilation = CSharpCompilation.Create("comp.dll", new[] { tree });
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var node = root.DescendantNodesAndSelf().OfType<T>().First();
            var context = new SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions, diag => { }, default(CancellationToken));
            return context;
        }

        private static SymbolAnalysisContext GetSymbolAnalysisContext<T>(string code, string fileName = "a.cs") where T : SyntaxNode
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var compilation = CSharpCompilation.Create("comp.dll", new[] { tree });
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var node = root.DescendantNodesAndSelf().OfType<T>().First();
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol == null) symbol = semanticModel.GetDeclaredSymbol(node);
            var context = new SymbolAnalysisContext(symbol, compilation, analyzerOptions, diag => { }, default(CancellationToken));
            return context;
        }

        private static SyntaxTreeAnalysisContext GetSyntaxTreeAnalysisContext(string code, string fileName = "a.cs")
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var context = new SyntaxTreeAnalysisContext(tree, analyzerOptions, diag => { }, default(CancellationToken));
            return context;
        }
    }
}