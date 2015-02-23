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
        public void IsContextOnGeneratedFile() =>
            GetContext("class TypeName { }", "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAClass() =>
            GetContext<ClassDeclarationSyntax>("[System.Diagnostics.DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasAbbreviatedDebuggerNonUserCodeAttributeOnAClass() =>
            GetContext<ClassDeclarationSyntax>("using System.Diagnostics; [DebuggerNonUserCode] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAMethod() =>
            GetContext<MethodDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod() =>
            GetContext<InvocationExpressionSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { System.Console.WriteLine(1); } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAProperty() =>
            GetContext<PropertyDeclarationSyntax>("class TypeName { [System.Diagnostics.DebuggerNonUserCode] int Foo { get; set; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAPropertyCheckGet() =>
            GetContext<AccessorDeclarationSyntax>("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCode] int Foo { get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAPropertyGet() =>
            GetContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] get { return foo; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasDebuggerNonUserCodeAttributeOnAPropertySet() =>
            GetContext<AccessorDeclarationSyntax>("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] set { foo = value; } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAClass() =>
            GetContext<ClassDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAnInterface() =>
            GetContext<InterfaceDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] interface ITypeName { }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnEnum() =>
            GetContext<EnumDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] enum A { a }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAField() =>
            GetContext<FieldDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] private int i; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAnEventField() =>
            GetContext<EventFieldDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event System.Action a; }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAnEventDeclaration() =>
            GetContext<EventDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAnEventDeclarationCheckingAccessor() =>
            GetContext<AccessorDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnAnEventDeclarationAccessor() =>
            GetContext<AccessorDeclarationSyntax>("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCode(null, null)] add { } remove { } } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnParameter() =>
            GetContext<ParameterSyntax>("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCode(null, null)] int i) { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnDelegate() =>
            GetContext<DelegateDeclarationSyntax>("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] delegate void A(); }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnReturnValue() =>
            GetContext<ReturnStatementSyntax>("class TypeName {[return: System.CodeDom.Compiler.GeneratedCode(null, null)] int Foo() { return 1; } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void HasGeneratedCodeAttributeOnNestedClass() =>
            GetContext<StructDeclarationSyntax>("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { struct Nested { } }").IsGenerated().Should().BeTrue();

        [Fact]
        public void WithAutoGeneratedCommentBasedOnWebForms() =>
            GetContext<ClassDeclarationSyntax>(
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
        public void WithAutoGeneratedCommentEmpty() =>
            GetContext(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------").IsGenerated().Should().BeTrue();

        private static SyntaxNodeAnalysisContext GetContext(string code, string fileName = "a.cs") => GetContext<CompilationUnitSyntax>(code, fileName);

        private static SyntaxNodeAnalysisContext GetContext<T>(string code, string fileName = "a.cs") where T : SyntaxNode
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
    }
}