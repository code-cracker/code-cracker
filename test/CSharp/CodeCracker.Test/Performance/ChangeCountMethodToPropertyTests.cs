using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class ChangeCountMethodToPropertyTests : CodeFixVerifier<ChangeCountMethodToPropertyAnalyzer, ChangeCountMethodToPropertyCodeFixProvider>
    {
        [Fact]
        public async Task ChangeCountPropertyIsUsed()
        {
            const string test = @"
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                public class Foo
                {
                    public void bar()
                    {
                        var myList = new List<string>();
                        var count = myList.Count;
                    }
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ChangeCountNoDiagnosticsWhenDifferentTypes()
        {
            const string test = @"
            class Program
            {
                static void Main(string[] args)
                {
                    var b = new Bar();
                    Foo(b.Count());
                }
                static void Foo(int i)
                {
                }

                class Bar : List<int>
                {
                    public string Count { get; set; }
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        public async Task ChangeCountToPropertyNeeded()
        {
            const string test = @"
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                public class Foo
                {
                    public void bar()
                    {
                        var myList = new List<string>();
                        var count = myList.Count();
                    }
                }
            }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ChangeCountMethodToProperty.ToDiagnosticId(),
                Message = ChangeCountMethodToPropertyAnalyzer.MessageFormat.ToString(),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 37) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        public async Task ChangeCountPropertyHasNoProperty()
        {
            const string test = @"
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                public class Foo
                {
                    public void bar()
                    {
                        var myList = new NoCountProperty();
                        var count = myList.Count();
                    }
                }
        
                public class NoCountProperty
                {
                    public int Count() => 0;
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ChangeCountFromMethodToProperty()
        {
            const string source = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                public class Foo
                {
                    public void bar()
                    {
                        var enumerable = Enumerable.Any(new[] { 1, 2 });
                        var myList = new List<string>();
                        var count = myList.Count();
                    }
                }
            }";

            const string expected = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApplication1
            {
                public class Foo
                {
                    public void bar()
                    {
                        var enumerable = Enumerable.Any(new[] { 1, 2 });
                        var myList = new List<string>();
                        var count = myList.Count;
                    }
                }
            }";

            await VerifyCSharpFixAsync(source, expected);
        }
    }
}