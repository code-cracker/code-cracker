using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class SwicthWithoutDefaultTests : CodeFixVerifier<SwitchWithoutDefaultAnalyzer, SwitchWithoutDefaultCodeFixProvider>
    {
        [Fact]
        public async Task SwithWithoutDefaultAnalyserBoolean()
        {
            const string source = @"using System;namespace 
                                    ConsoleApplication1 
                                    {
                                        class TypeName
                                        {
                                              static void Main()
                                              {
                                                 var s = false;
                                                 switch (s)
                                                 {
                                                  case false;break:{break;}                                                  
                                                 }
                                              }
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

        [Fact]
        public async Task SwithWithoutDefaultAnalyserNoTesteDiagnostic()
        {
            const string test = @"
            using System;

            namespace ConsoleApplication1
            {
                class TypeName
                {
                    int x;
                    public void Foo()
                    {
                        var aas = """";                        
                        switch (aas)
                        {
                            case """":
                                break;                
                        }
                    }
                }
            }";

            const string fixtest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        int x;
        public void Foo()
        {
            var aas = """";
            switch (aas)
            {
                case """":
break;
                default:
                    throw new Exception(""Unexpected Case"");
            }
        }
    }
}";

            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: true);
        }

    }
}
