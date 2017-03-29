using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class SwicthWithoutDefaultTests : CodeFixVerifier<SwitchWithoutDefaultAnalyzer, SwitchWithoutDefaultCodeFixProvider>
    {
        [Fact]
        public async Task SwithWithoutDefaultAnalyserString()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
var a = """";
            switch (a)// c1
            {// c2
                case """":
                    break;
            }// c3
                                              }
                                        }
                                   }";

            const string fixtest = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
var a = """";
            switch (a)// c1
            {// c2
                case """":
                    break;
                default:
                    throw new Exception(""Unexpected Case"");
            }// c3
    }
                                        }
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwithWithoutDefaultAnalyserBool()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                var s = true;
                                                switch (s)
                                                {
                                                    case false:
                                                    break;
                                                }
                                        }
                                   }";

            const string fixtest = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                var s = true;
                                                switch (s)
                                                {
                                                    case false:
                                                        break;
                                                    case true:
                                                        break;
                                                }
                                        }
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwitchWithoutDefaultAnalyserInt()
        {
            const string source = @"using System;
                                    namespace ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                    var s = 10;
                                                    switch (s)
                                                    {
                                                        case 10:
                                                            break;
                                                    }
                                               }
                                         }
                                   }";

            const string fixtest = @"using System;
                                     namespace ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                    var s = 10;
                                                    switch (s)
                                                    {
                                                        case 10:
                                                            break;
                                                        default:
                                                            throw new Exception(""Unexpected Case"");
                                                    }
                                            }
                                        }
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwitchWithoutDefaultAnalyserBoolMethod()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              void Bar(bool myBool)
        {
            switch (myBool)
            {
                case false:
                    break;
            }
        }
                                        }
                                   }";

            const string fixtest = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                            void Bar(bool myBool)
        {
            switch (myBool)
            {
                case false:
                    break;
                case true:
                    break;
            }
        }
                                        }
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwithWithoutDefaultAnalyserIntMethod()
        {
            const string source = @"namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
 void Bar3(int a)
        {
            switch (a)
            {
                case 10:
                    break;
            }
        }
                                        }
                                   }";

            const string fixtest = @"namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
         void Bar3(int a)
        {
            switch (a)
            {
                case 10:
                    break;
                default:
                    throw new System.Exception(""Unexpected Case"");
            }
       }
}
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwitchWithCast()
        {
            const string source = @"
using System;
class TypeName
{
    void Bar()
    {
        var t = new TypeName();
        switch ((int)t)
        {
            case 1: break;
        }
    }
    public static explicit operator int(TypeName v) => 1;
}";

            const string fixtest = @"
using System;
class TypeName
{
    void Bar()
    {
        var t = new TypeName();
        switch ((int)t)
        {
            case 1: break;
            default:
                throw new Exception(""Unexpected Case"");
        }
    }
    public static explicit operator int(TypeName v) => 1;
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwithWithoutDefaultAnalyserIntMethodTwoParams()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
void Bar4(int a, int b)
        {
            switch (a)
            {
                case 10:
                    break;
            }
        }
                                        }
                                   }";

            const string fixtest = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
void Bar4(int a, int b)
        {
            switch (a)
            {
                case 10:
                    break;
                default:
                    throw new Exception(""Unexpected Case"");
            }
    }
}
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwithWithoutDefaultAnalyserIntMethodStatic()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
static void Bar5(int a)
        {
            switch (a)
            {
                case 10:
                    break;
            }
        }

                                        }
                                   }";

            const string fixtest = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
static void Bar5(int a)
        {
            switch (a)
            {
                case 10:
                    break;
                default:
                    throw new Exception(""Unexpected Case"");
            }
    }

}
                                   }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task SwithWithoutDefaultAnalyseCS0151Exception()
        {
            const string source = @"static void M() { }
        static void Main()
        {
            switch (M()) // CS0151
            {
                default:
                    break;
            }
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }


        [Fact]
        public async Task SwithWithoutDefaultAnalyserNoDiagnostic()
        {
            const string source = @"using System;namespace
                                    ConsoleApplication1
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                var s = "";
                                                 switch (s)
                                                 {
                                                  case "":{break;}
                                                  default:{break;}
                                                 }
                                        }
                                      }
                                   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task SwitchWithoutDefaultAnalyzerNoDiagnosticWithCompileError()
        {
            const string source = @"using System;
                                    namespace ConsoleApplication1
                                    {
                                        ConsoleApplication1
                                        {
                                            class Teste { }
                                            class Program
                                            {
                                                static void Main(string[] args)
                                                {
                                                    Teste vo_teste = new Teste();
                                                    switch (vo_teste)
                                                    {
                                                        case "":break;
                                                        default:break;
                                                    }
                                                }
                                            }
                                        }
                                   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}