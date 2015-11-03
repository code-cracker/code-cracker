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

namespace CodeCracker.Test.CSharp.Design.InconsistentAccessibilityTests
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        private readonly AccessibilityDomainChecker accessibilityDomainChecker = new AccessibilityDomainChecker();

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

            var fixInfo = await AccessibilityDomainCheckerFixInfoFor(testCode).ConfigureAwait(false);

            Assert.Equal("InternalClass", fixInfo.Type.ToString());
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

            var fixInfo = await AccessibilityDomainCheckerFixInfoFor(testCode).ConfigureAwait(false);

            Assert.Equal("InternalClass", fixInfo.Type.ToString());
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

            var fixInfo = await AccessibilityDomainCheckerFixInfoFor(testCode).ConfigureAwait(false);

            Assert.Equal("InternalClass.NestedInternalClass", fixInfo.Type.ToString());
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
            var fixInfo = await AccessibilityDomainCheckerFixInfoFor(testCode).ConfigureAwait(false);

            Assert.Equal("InternalClass.NestedInternalClass.NestedNestedPublicClass", fixInfo.Type.ToString());

        }
    }
}
