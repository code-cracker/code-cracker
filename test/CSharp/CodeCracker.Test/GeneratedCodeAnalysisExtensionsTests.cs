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
        private const string baseProjectPath = @"D:\ClassLibrary11\";

        [Theory]
        [InlineData(baseProjectPath + "A.g.CS")]
        [InlineData(baseProjectPath + "A.g.cs")]
        [InlineData(baseProjectPath + "B.g.cs")]
        [InlineData(baseProjectPath + "A.g.i.cs")]
        [InlineData(baseProjectPath + "B.g.i.cs")]
        [InlineData(baseProjectPath + "A.designer.cs")]
        [InlineData(baseProjectPath + "A.generated.cs")]
        [InlineData(baseProjectPath + "B.generated.cs")]
        [InlineData(baseProjectPath + "AssemblyInfo.cs")]
        [InlineData(baseProjectPath + "A.AssemblyAttributes.cs")]
        [InlineData(baseProjectPath + "B.AssemblyAttributes.cs")]
        [InlineData(baseProjectPath + "AssemblyAttributes.cs")]
        [InlineData(baseProjectPath + "Service.cs")]
        [InlineData(baseProjectPath + "TemporaryGeneratedFile_.cs")]
        [InlineData(baseProjectPath + "TemporaryGeneratedFile_A.cs")]
        [InlineData(baseProjectPath + "TemporaryGeneratedFile_B.cs")]
        public static void IsOnGeneratedFile(string fileName) => fileName.IsOnGeneratedFile().Should().BeTrue();

        [Theory]
        [InlineData(baseProjectPath + "TheAssemblyInfo.cs")]
        [InlineData(baseProjectPath + "A.cs")]
        [InlineData(baseProjectPath + "TheTemporaryGeneratedFile_A.cs")]
        [InlineData(baseProjectPath + "TheService.cs")]
        [InlineData(baseProjectPath + "TheAssemblyAttributes.cs")]
        public static void IsNotOnGeneratedFile(string fileName) => fileName.IsOnGeneratedFile().Should().BeFalse();

        [Fact]
        public static void SyntaxNodeAnalysis_IsContextOnGeneratedFile() =>
            GetSyntaxNodeAnalysisContext("class TypeName { }", baseProjectPath + "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public static void SymbolicAnalysis_IsContextOnGeneratedFile() =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>("class TypeName { }", baseProjectPath + "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Fact]
        public static void SyntaxTreeAnalysis_IsContextOnGeneratedFile() =>
            GetSyntaxTreeAnalysisContext("class TypeName { }", baseProjectPath + "TemporaryGeneratedFile_.cs").IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.Diagnostics.DebuggerNonUserCode] class TypeName { }")]
        [InlineData("[System.Diagnostics.DebuggerNonUserCodeAttribute] class TypeName { }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAClass(string source) =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.Diagnostics.DebuggerNonUserCode] class TypeName { }")]
        [InlineData("[System.Diagnostics.DebuggerNonUserCodeAttribute] class TypeName { }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAClass(string source) =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("using System.Diagnostics; [DebuggerNonUserCode] class TypeName { }")]
        [InlineData("using System.Diagnostics; [DebuggerNonUserCodeAttribute] class TypeName { }")]
        public static void SyntaxNodeAnalysis_HasAbbreviatedDebuggerNonUserCodeAttributeOnAClass(string source) =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("using System.Diagnostics; [DebuggerNonUserCode] class TypeName { }")]
        [InlineData("using System.Diagnostics; [DebuggerNonUserCodeAttribute] class TypeName { }")]
        public static void SymbolicAnalysis_HasAbbreviatedDebuggerNonUserCodeAttributeOnAClass(string source) =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] void Foo() { } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAMethod(string source) =>
            GetSyntaxNodeAnalysisContext<MethodDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] void Foo() { } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAMethod(string source) =>
            GetSymbolAnalysisContext<MethodDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { System.Console.WriteLine(1); } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] void Foo() { System.Console.WriteLine(1); } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod(string source) =>
            GetSyntaxNodeAnalysisContext<InvocationExpressionSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] void Foo() { var i = 1; } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] void Foo() { var i = 1; } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAStatementWithinAMethod(string source) =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] int Foo { get; set; } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] int Foo { get; set; } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAProperty(string source) =>
            GetSyntaxNodeAnalysisContext<PropertyDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCode] int Foo { get; set; } }")]
        [InlineData("class TypeName { [System.Diagnostics.DebuggerNonUserCodeAttribute] int Foo { get; set; } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAProperty(string source) =>
            GetSymbolAnalysisContext<PropertyDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCode] int Foo { get { return foo; } } }")]
        [InlineData("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCodeAttribute] int Foo { get { return foo; } } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyCheckGet(string source) =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCode] int Foo { get { return foo; } } }")]
        [InlineData("class TypeName { private int foo; [System.Diagnostics.DebuggerNonUserCodeAttribute] int Foo { get { return foo; } } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyCheckGet(string source) =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] get { return foo; } } }")]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCodeAttribute] get { return foo; } } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyGet(string source) =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] get { return foo; } } }")]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCodeAttribute] get { return foo; } } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertyGet(string source) =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] set { foo = value; } } }")]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCodeAttribute] set { foo = value; } } }")]
        public static void SyntaxNodeAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertySet(string source) =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCode] set { foo = value; } } }")]
        [InlineData("class TypeName { private int foo; int Foo { [System.Diagnostics.DebuggerNonUserCodeAttribute] set { foo = value; } } }")]
        public static void SymbolicAnalysis_HasDebuggerNonUserCodeAttributeOnAPropertySet(string source) =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] class TypeName { }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAClass(string source) =>
            GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] class TypeName { }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAClass(string source) =>
            GetSymbolAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] interface ITypeName { }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] interface ITypeName { }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnInterface(string source) =>
            GetSyntaxNodeAnalysisContext<InterfaceDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] interface ITypeName { }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] interface ITypeName { }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnInterface(string source) =>
            GetSymbolAnalysisContext<InterfaceDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] enum A { a }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] enum A { a }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnEnum(string source) =>
            GetSyntaxNodeAnalysisContext<EnumDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] enum A { a }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] enum A { a }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnEnum(string source) =>
            GetSymbolAnalysisContext<EnumDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] private int i; }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] private int i; }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAField(string source) =>
            GetSyntaxNodeAnalysisContext<FieldDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] private int i; }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] private int i; }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAField(string source) =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event System.Action a; }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event System.Action a; }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventField(string source) =>
            GetSyntaxNodeAnalysisContext<EventFieldDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event System.Action a; }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event System.Action a; }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventField(string source) =>
            GetSymbolAnalysisContext<VariableDeclaratorSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event EventHandler A { add { } remove { } } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclaration(string source) =>
            GetSyntaxNodeAnalysisContext<EventDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event EventHandler A { add { } remove { } } }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclaration(string source) =>
            GetSymbolAnalysisContext<EventDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event EventHandler A { add { } remove { } } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationCheckingAccessor(string source) =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] event EventHandler A { add { } remove { } } }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] event EventHandler A { add { } remove { } } }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationCheckingAccessor(string source) =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCode(null, null)] add { } remove { } } }")]
        [InlineData("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] add { } remove { } } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationAccessor(string source) =>
            GetSyntaxNodeAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCode(null, null)] add { } remove { } } }")]
        [InlineData("class TypeName { event EventHandler A { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] add { } remove { } } }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnAnEventDeclarationAccessor(string source) =>
            GetSymbolAnalysisContext<AccessorDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCode(null, null)] int i) { } }")]
        [InlineData("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] int i) { } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnParameter(string source) =>
            GetSyntaxNodeAnalysisContext<ParameterSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCode(null, null)] int i) { } }")]
        [InlineData("class TypeName { void Foo([System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] int i) { } }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnParameter(string source) =>
            GetSymbolAnalysisContext<ParameterSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] delegate void A(); }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] delegate void A(); }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnDelegate(string source) =>
            GetSyntaxNodeAnalysisContext<DelegateDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCode(null, null)] delegate void A(); }")]
        [InlineData("class TypeName { [System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] delegate void A(); }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnDelegate(string source) =>
            GetSymbolAnalysisContext<DelegateDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("class TypeName {[return: System.CodeDom.Compiler.GeneratedCode(null, null)] int Foo() { return 1; } }")]
        [InlineData("class TypeName {[return: System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] int Foo() { return 1; } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnReturnValue(string source) =>
            GetSyntaxNodeAnalysisContext<ReturnStatementSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { struct Nested { } }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] class TypeName { struct Nested { } }")]
        public static void SyntaxNodeAnalysis_HasGeneratedCodeAttributeOnNestedClass(string source) =>
            GetSyntaxNodeAnalysisContext<StructDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData("[System.CodeDom.Compiler.GeneratedCode(null, null)] class TypeName { struct Nested { } }")]
        [InlineData("[System.CodeDom.Compiler.GeneratedCodeAttribute(null, null)] class TypeName { struct Nested { } }")]
        public static void SymbolicAnalysis_HasGeneratedCodeAttributeOnNestedClass(string source) =>
            GetSymbolAnalysisContext<StructDeclarationSyntax>(source).IsGenerated().Should().BeTrue();

        [Theory]
        [InlineData(
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
}", false)]
        [InlineData(
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------", true)]
        [InlineData(
@"// <auto-generated>
// This code was generated by a tool.
namespace WebApplication3
{
    public partial class _Default
    {
    }
}", false)]
        [InlineData(
@"// <auto-generated>
// This code was generated by a tool.", true)]
        [InlineData(
@"// This code was generated by a tool.
//
// <auto-generated>
namespace WebApplication3
{
    public partial class _Default
    {
    }
}", false)]
        [InlineData(
@"// This code was generated by a tool.
//
// <auto-generated>", true)]
        public static void SyntaxNodeAnalysis_WithAutoGeneratedComment(string source, bool isEmpty)
        {
            GetSyntaxTreeAnalysisContext(source).IsGenerated().Should().BeTrue();
            if (isEmpty)
            {
                GetSyntaxNodeAnalysisContext(source).IsGenerated().Should().BeTrue();
            }
            else
            {
                GetSyntaxNodeAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();
                GetSymbolAnalysisContext<ClassDeclarationSyntax>(source).IsGenerated().Should().BeTrue();
            }
        }

        private static SyntaxNodeAnalysisContext GetSyntaxNodeAnalysisContext(string code, string fileName = baseProjectPath + "a.cs") => GetSyntaxNodeAnalysisContext<CompilationUnitSyntax>(code, fileName);

        private static SyntaxNodeAnalysisContext GetSyntaxNodeAnalysisContext<T>(string code, string fileName = baseProjectPath + "a.cs") where T : SyntaxNode
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var compilation = CSharpCompilation.Create("comp.dll", new[] { tree });
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var node = root.DescendantNodesAndSelf().OfType<T>().First();
            var context = new SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions, diag => { }, diag => true, default(CancellationToken));
            return context;
        }

        private static SymbolAnalysisContext GetSymbolAnalysisContext<T>(string code, string fileName = baseProjectPath + "a.cs") where T : SyntaxNode
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var compilation = CSharpCompilation.Create("comp.dll", new[] { tree });
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var node = root.DescendantNodesAndSelf().OfType<T>().First();
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol == null) symbol = semanticModel.GetDeclaredSymbol(node);
            var context = new SymbolAnalysisContext(symbol, compilation, analyzerOptions, diag => { }, diag => true, default(CancellationToken));
            return context;
        }

        private static SyntaxTreeAnalysisContext GetSyntaxTreeAnalysisContext(string code, string fileName = baseProjectPath + "a.cs")
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, path: fileName);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var context = new SyntaxTreeAnalysisContext(tree, analyzerOptions, diag => { }, diag => true, default(CancellationToken));
            return context;
        }
    }
}