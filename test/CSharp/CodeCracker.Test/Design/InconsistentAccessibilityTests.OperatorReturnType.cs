using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        [InlineData("~")]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("!")]
        public async Task ShouldFixInconsistentAccessibilityInUnaryOperatorAsync(string unaryOperator)
        {
            var testCode = @"public class Base
    {
        public static Derived operator " + unaryOperator + @"(Base m)
        {
            return new Derived();
        }
    }

    internal class Derived : Base
    {

    }";

            var fixedCode = @"public class Base
    {
        public static Derived operator " + unaryOperator + @"(Base m)
        {
            return new Derived();
        }
    }

    public class Derived : Base
    {

    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        public async Task ShouldFixInconsistentAccessibilityInBinaryOperatorAsync(string binaryOperator)
        {
            var testCode = @"public class Base
    {
        public static Derived operator " + binaryOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }
    }

    internal class Derived : Base
    {
        
    }";

            var fixedCode = @"public class Base
    {
        public static Derived operator " + binaryOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }
    }

    public class Derived : Base
    {
        
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("==", "!=")]
        [InlineData("<", ">")]
        [InlineData(">=", "<=")]
        public async Task ShouldFixInconsistentAccessibilityInPairwiseBinaryOperatorAsync(string binaryOperator,
            string matchingOperator)
        {
            var testCode = @"public class Base
    {
        public static Derived operator " + binaryOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }

        public static Derived operator " + matchingOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }
    }

    internal class Derived : Base
    {
        
    }";

            var fixedCode = @"public class Base
    {
        public static Derived operator " + binaryOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }

        public static Derived operator " + matchingOperator + @"(Base m1, Base m2)
        {
            return new Derived();
        }
    }

    public class Derived : Base
    {
        
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        public async Task ShouldFixInconsistentAccessibilityInShifBinaryOperatorAsync(string shiftBinaryOperator)
        {
            var testCode = @"public class Base
    {
        public static Derived operator " + shiftBinaryOperator + @"(Base m1, int m2)
        {
            return new Derived();
        }
    }

    internal class Derived : Base
    {
        
    }";

            var fixedCode = @"public class Base
    {
        public static Derived operator " + shiftBinaryOperator + @"(Base m1, int m2)
        {
            return new Derived();
        }
    }

    public class Derived : Base
    {
        
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInConversionOperatorAsync()
        {
            var testCode = @"public class Money
    {
        public static implicit operator SomeClass(Money m)
        {
            return new SomeClass();
        }
    }

    internal class SomeClass
    {

    }";

            var fixedCode = @"public class Money
    {
        public static implicit operator SomeClass(Money m)
        {
            return new SomeClass();
        }
    }

    public class SomeClass
    {
        
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
