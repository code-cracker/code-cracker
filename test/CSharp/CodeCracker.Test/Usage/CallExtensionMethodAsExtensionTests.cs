using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class CallExtensionMethodAsExtensionTests :
        CodeFixVerifier<CallExtensionMethodAsExtensionAnalyzer, CallExtensionMethodAsExtensionCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreWhenMissingArguments()
        {
            const string source = @"
using System.Linq;
class Foo
{
    public static void N()
    {
        Exts.M();
    }
}
static class Exts
{
    public static void M(this string s) => s.ToString();
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsExtensionHasNoDiagnostics()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any(x => x > 1);
                            }
                        }
                    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMethodTriggerAFix()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 33)
                .WithMessage("Do not call 'Any' method of class 'Enumerable' as a static method");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMethodTriggerAFixWithCSharp5()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";
            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 33)
                .WithMessage("Do not call 'Any' method of class 'Enumerable' as a static method");
            await VerifyCSharpDiagnosticAsync(source, expected, LanguageVersion.CSharp5);
        }

        [Fact]
        public async Task CreatesDiagnosticWhenExtensionClassInSameTree()
        {
            const string source = @"
class Foo
{
    public static void Bar()
    {
        C.M("""");
    }
}
public static class C
{
    public static string M(this string s) => s;
}";
            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 9)
                .WithMessage("Do not call 'M' method of class 'C' as a static method");
            await VerifyCSharpDiagnosticAsync(source, expected, LanguageVersion.CSharp5);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithFullNamespaceAsStaticMenthodTriggerAFix()
        {
            const string source = @"
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                System.Linq.Enumerable.Any(source);
                            }
                        }
                    }";

            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(9, 33)
                .WithMessage("Do not call 'Any' method of class 'Enumerable' as a static method");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMenthodShouldFix()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            const string expected = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any(x => x > 1);
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMethodShouldFixWithReturnStatement()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public bool Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                return Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            const string expected = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public bool Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                return source.Any(x => x > 1);
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithFullNamespaceAndNotImportedShouldImport()
        {
            const string source = @"
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;

                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                System.Linq.Enumerable.Any(source);
                            }
                        }
                    }";

            const string expected = @"
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any();
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithChainCallsShouldNotBreakTheChain()
        {
            const string source = @"
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Select(source, (p) => p.ToString()).FirstOrDefault();
                            }
                        }
                    }";

            const string expected = @"
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Select((p) => p.ToString()).FirstOrDefault();
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithDynamicArgumentHasNoDiagnostics()
        {
            const string source = @"
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        class ExtensionTest
                        {
                            void method()
                            {
                                var list = new List<int>() { 5, 56, 2, 4, 63, 2 };
                                dynamic dList = list;
                                Console.WriteLine(Enumerable.First(dList));
                            }
                        }
                    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenTheSelectedMethodWouldChangeThenCreatesNoDiagnostics()
        {
            const string source = @"
using System.Linq;
using System.Collections.Generic;
public static class ExtensionsTestCase
{
    public static void Select(this IEnumerable<string> strings, System.Func<string, bool> mySelector)
    {
    }
    public static void UsesSelect()
    {
        Enumerable.Select(new[] { """" }, s => s == """");
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMethodInsideForDoesNotThrowAndCreatesDiagnostic()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                for (int i = 0; i < 10; i++)
                                    Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(11, 37)
                .WithMessage("Do not call 'Any' method of class 'Enumerable' as a static method");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }


        [Fact]
        public async Task WhenCallExtensionMethodWithoutInvocationStatement()
        {
            const string source = @"
              using System.Linq;
              namespace ConsoleApplication1
              {
                  public class Foo
                  {
                      static int[] source = new int[] { 1, 2, 3 };
                      public static void Bar() => Enumerable.Any(source);
                  }
              }";

            var expected = new DiagnosticResult(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(8, 51)
                .WithMessage("Do not call 'Any' method of class 'Enumerable' as a static method");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}