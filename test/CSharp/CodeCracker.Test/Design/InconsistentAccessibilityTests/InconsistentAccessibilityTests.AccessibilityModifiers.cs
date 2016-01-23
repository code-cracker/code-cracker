using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CodeCracker.Test.CSharp.Design.InconsistentAccessibilityTests
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        private readonly AccessibilityModifiersEvaluator accessibilityModifiersEvaluator =
            new AccessibilityModifiersEvaluator(new NoOpFixInfoProviderStub());

        [Fact]
        public async Task ShouldCreatePublicModifierForProtectedInconsistentAccessibilitySourceWhenSourceIsInNestedTypeAsync()
        {
            var testCode = @"class InternalClass { }
public class ClassWithProtectedDelegate
{
    protected delegate void ProtectedDelegate(InternalClass x);
}";

            var fixInfo =
                await AccessibilityModifiersEvaluatorFixInfoFor(testCode, ProtectedModifier()).ConfigureAwait(false);

            AssertFixHasModifier(fixInfo, SyntaxKind.PublicKeyword);
        }

        [Fact]
        public async Task ShouldCreatePublicModifierForProtectedSourceWhenTypeToChangeIsNestedInHigherClassAndAlsoProtectedAsync()
        {
            var testCode = @"public class ClassWithNestedClasses
{
    class InternalNestedClass
    {

    }
    public class NestedClassWithProtectedDelegate
    {
        protected delegate void ProtectedDelegate(InternalNestedClass x);
    }
}";

            var fixInfo =
                await AccessibilityModifiersEvaluatorFixInfoFor(testCode, ProtectedModifier()).ConfigureAwait(false);

            AssertFixHasModifier(fixInfo, SyntaxKind.PublicKeyword);
        }

        [Fact]
        public async Task ShouldCreateProtectedModifierForProtectedSourceWhenDestinationAndSourceInTheSameScope()
        {
            var testCode = @"public class PublicClassWithInconsistentAccessibileMembersAtTheSameScope
{
    class InternalNestedClass
    {

    }

    protected delegate void ProtectedDelegate(InternalNestedClass x);
}";
            var fixInfo =
                await AccessibilityModifiersEvaluatorFixInfoFor(testCode, ProtectedModifier()).ConfigureAwait(false);

            AssertFixHasModifier(fixInfo, SyntaxKind.ProtectedKeyword);
        }

        private static SyntaxTokenList ProtectedModifier()
            => SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));

        private static void AssertFixHasModifier(InconsistentAccessibilityFixInfo fixInfo, SyntaxKind modifier)
            => Assert.Collection(fixInfo.Modifiers, token => Assert.Equal(modifier, token.Kind()));

        private class NoOpFixInfoProviderStub : IInconsistentAccessibilityFixInfoProvider
        {
            public Task<InconsistentAccessibilityFixInfo> CreateFixInfoAsync(CodeFixContext context, InconsistentAccessibilitySource source)
            {
                return Task.FromResult(new InconsistentAccessibilityFixInfo(source.TypeToChangeAccessibility, source.Modifiers));
            }
        }
    }
}
