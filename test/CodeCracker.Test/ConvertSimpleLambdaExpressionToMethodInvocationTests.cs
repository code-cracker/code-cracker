using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ConvertSimpleLambdaExpressionToMethodInvocationTests
        :CodeFixTest<ConvertSimpleLambdaExpressionToMethodInvocationAnalizer, ConvertSimpleLambdaExpressionToMethodInvocationFixProvider>
    {
        [Fact]
        public void CreateDiagnosticWhenUsingWhereWithLambda()
        {
            string test = @"var f = a.Where(item => filter(item));";
            var expected = new DiagnosticResult
            {
                Id = ConvertSimpleLambdaExpressionToMethodInvocationAnalizer.DiagnosticId,
                Message = "You should remove the lambda expression and pass just 'filter' instead.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 17) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DoNotCreateDiagnosticWhenUsingWhereWithoutLambda()
        {
            string test = @"var f = a.Where(filter);";
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void SimpleLambdaExpressionIsReplaceByMethodInDeclarationStatement()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, bool> a = x => filter(x);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, bool> a = filter;
        }
    }
}";

            VerifyCSharpFix(oldCode, newCode);
        }


        [Fact]
        public void SimpleLambdaExpressionIsReplaceByMethodInArgumentList()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(item => filter(item));
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            VerifyCSharpFix(oldCode, newCode);
        }


        [Fact]
        public void FixEndOfPipelineLambdaExpressionAndReplaceByMethod()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => filter(item));
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter);
        }
    }
}";

            VerifyCSharpFix(oldCode, newCode);
        }


        [Fact]
        public void FixMiddleOfPipelineLambdaExpressionAndReplaceByMethod()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => filter(item)).Select(item => item * 2);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter).Select(item => item * 2);
        }
    }
}";

            VerifyCSharpFix(oldCode, newCode);
        }


        [Fact]
        public void FixMiddleOfPipelineLambdaExpressionAndReplaceByMethodMultipleMatches()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        private int orderAccessor(int value)
        {
            return item;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => orderAccessor(item)).Where(item => filter(item)).Select(item => item * 2);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        private int orderAccessor(int value)
        {
            return item;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(orderAccessor).Where(filter).Select(item => item * 2);
        }
    }
}";

            VerifyCSharpFix(oldCode, newCode);
        }
    }
}
