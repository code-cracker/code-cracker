using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class InterfaceNameTests : CodeFixVerifier<InterfaceNameAnalyzer, InterfaceNameCodeFixProvider>
    {
        [Fact]
        public async Task InterfaceNameStartsWithLetterI()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task InterfaceNameNotStartsWithLetterI()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.InterfaceName.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 9)
                .WithMessage(InterfaceNameAnalyzer.MessageFormat);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeInterfaceNameWithoutI()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task ChangeInterfaceNameWithoutIAndClassImplementation()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }

        public class Test : Foo
        {
            public void Test()
            {

            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }

        public class Test : IFoo
        {
            public void Test()
            {

            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeAllInterfaceNamesWithoutI()
        {
            const string source1 = @"
    namespace ConsoleApplication1
    {
        public interface Foo1
        {
            void Test();
        }
    }";
            const string source2 = @"
    namespace ConsoleApplication2
    {
        public interface Foo2
        {
            void Test();
        }
    }";
            const string fixtest1 = @"
    namespace ConsoleApplication1
    {
        public interface IFoo1
        {
            void Test();
        }
    }";
            const string fixtest2 = @"
    namespace ConsoleApplication2
    {
        public interface IFoo2
        {
            void Test();
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source1, source2 }, new string[] { fixtest1, fixtest2 });
        }


        [Fact]
        public async Task ChangeAllInterfaceNamesWithoutIAndClassImplementation()
        {
            const string source1 = @"
    using ConsoleApplication2;
    namespace ConsoleApplication1
    {
        public interface Foo2
        {
            void Test();
        }

        public class Test1 : Foo1
        {
            public void Test()
            {

            }
        }
    }";
            const string source2 = @"
    using ConsoleApplication1;
    namespace ConsoleApplication2
    {
        public interface Foo1
        {
            void Test();
        }

        public class Test2 : Foo2
        {
            public void Test()
            {

            }
        }
    }";
            const string fixtest1 = @"
    using ConsoleApplication2;
    namespace ConsoleApplication1
    {
        public interface IFoo2
        {
            void Test();
        }

        public class Test1 : IFoo1
        {
            public void Test()
            {

            }
        }
    }";
            const string fixtest2 = @"
    using ConsoleApplication1;
    namespace ConsoleApplication2
    {
        public interface IFoo1
        {
            void Test();
        }

        public class Test2 : IFoo2
        {
            public void Test()
            {

            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source1, source2 }, new string[] { fixtest1, fixtest2 });
        }
    }
}