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
        public async Task CreateDiagnosticWhenUsingWhereWithLambda()
        {
            var test = @"var f = a.Where(item => filter(item));";
            var expected = new DiagnosticResult
            {
                Id = ConvertSimpleLambdaExpressionToMethodInvocationAnalizer.DiagnosticId,
                Message = "You should remove the lambda expression and pass just 'filter' instead.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task DoNotCreateDiagnosticWhenUsingWhereWithoutLambda()
        {
            var test = @"var f = a.Where(filter);";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IncompleteSimpleLambdaExpressionIsReplaceByMethodInDeclarationStatement()
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public Task bar()
        {
            var a = new int[10];
            Func<int, bool> a = x => filter();
        }
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task SimpleLambdaExpressionIsReplaceByMethodInDeclarationStatement()
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

        public Task bar()
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

        public Task bar()
        {
            var a = new int[10];
            Func<int, bool> a = filter;
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task SimpleLambdaExpressionIsReplaceByMethodInArgumentList()
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

        public Task bar()
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

        public Task bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task FixEndOfPipelineLambdaExpressionAndReplaceByMethod()
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

        public Task bar()
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

        public Task bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task FixMiddleOfPipelineLambdaExpressionAndReplaceByMethod()
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

        public Task bar()
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

        public Task bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter).Select(item => item * 2);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task FixMiddleOfPipelineLambdaExpressionAndReplaceByMethodMultipleMatches()
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

        public Task bar()
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

        public Task bar()
        {
            var a = new int[10];
            var f = a.OrderBy(orderAccessor).Where(filter).Select(item => item * 2);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }
    }
}
