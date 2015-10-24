using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        private readonly InconsistentAccessibilityCodeFixStub stub = new InconsistentAccessibilityCodeFixStub();

        [Fact]
        public async Task
            ShouldOverwriteTypeToChangeAccessibilityIfInconsistentAccessibilityIsCausedByAccessibilityDomainAsync()
        {
            const string testCode =
                @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedPublicClass param);

internal class InternalClass
{
    public class NestedPublicClass { }
}";

            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var sut = new AccessibilityDomainChecker(stub);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            await
                sut.FixAsync(context, diagnostic, CreateInfoFor(typeSyntax))
                    .ConfigureAwait(false);

            AssertThatTypeToChangeAccessibilityIs("InternalClass");
        }

        [Fact]
        public async Task
    ShouldHandleDeepNestingWhenCheckingAccessibilityDomainAsync()
        {
            const string testCode =
                @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedInternalClass.NestedNestedPublicClass param);

internal class InternalClass
{
    public class NestedInternalClass 
    {
        public class NestedNestedPublicClass { }
    }
}";

            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var sut = new AccessibilityDomainChecker(stub);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            await
                sut.FixAsync(context, diagnostic, CreateInfoFor(typeSyntax))
                    .ConfigureAwait(false);

            AssertThatTypeToChangeAccessibilityIs("InternalClass");
        }

        [Fact]
        public async Task
            ShouldHandleNestedInconsistentAccessibilityWhenCheckingAccessibilityDomainAsync()
        {
            const string testCode =
                @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedInternalClass.NestedNestedPublicClass param);

internal class InternalClass
{
    internal class NestedInternalClass 
    {
        public class NestedNestedPublicClass { }
    }
}";

            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var sut = new AccessibilityDomainChecker(stub);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            await
                sut.FixAsync(context, diagnostic, CreateInfoFor(typeSyntax))
                    .ConfigureAwait(false);

            AssertThatTypeToChangeAccessibilityIs("InternalClass.NestedInternalClass");
            AssertThatTypeToChangeAccessibilityIs("InternalClass");
        }

        [Fact]
        public async Task
    ShouldNotOverwriteTypeToChangeAccessibilityIfInconsitentAccessibilityIsNotInAccessibilityDomainAsync()
        {
            const string testCode =
                @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedInternalClass.NestedNestedPublicClass param);

public class InternalClass
{
    public class NestedInternalClass 
    {
        internal class NestedNestedPublicClass { }
    }
}";

            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var sut = new AccessibilityDomainChecker(stub);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            await
                sut.FixAsync(context, diagnostic, CreateInfoFor(typeSyntax))
                    .ConfigureAwait(false);

            AssertThatTypeToChangeAccessibilityIs("InternalClass.NestedInternalClass.NestedNestedPublicClass");
        }

        private void AssertThatTypeToChangeAccessibilityIs(string name)
        {
            Assert.Contains(stub.PassedInfo,
                accessibilityInfo => string.Equals(accessibilityInfo.TypeToChangeAccessibility.ToString(), name));
        }

        private static InconsistentAccessibilityInfo CreateInfoFor(TypeSyntax typeSyntax)
            =>
                new InconsistentAccessibilityInfo
                {
                    NewAccessibilityModifiers = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    TypeToChangeAccessibility = typeSyntax
                };

        private static Diagnostic CreateCs0059DiagnosticForDelegateDeclaration(IEnumerable<SyntaxNode> descendants)
        {
            var delegateDeclaration = descendants.OfType<DelegateDeclarationSyntax>().Single();

            return
                Diagnostic.Create(
                    new DiagnosticDescriptor("CS0059", "Title", "Format", "Category", DiagnosticSeverity.Error, true),
                    delegateDeclaration.GetLocation());
        }

        private static TypeSyntax GetTypeSyntaxFromFirstParameterInDelegateDeclaration(
            IEnumerable<SyntaxNode> descendants) =>
                descendants.OfType<DelegateDeclarationSyntax>().Single().ParameterList.Parameters.First().Type;
    }

    public class InconsistentAccessibilityCodeFixStub : IInconsistentAccessibilityCodeFix
    {
        public InconsistentAccessibilityCodeFixStub()
        {
            PassedInfo = new List<InconsistentAccessibilityInfo>();
        }

        public List<InconsistentAccessibilityInfo> PassedInfo { get; private set; }

        public async Task FixAsync(CodeFixContext context, Diagnostic diagnostic, InconsistentAccessibilityInfo info)
        {
            PassedInfo.Add(info);

            await Task.FromResult(true);
        }
    }
}
