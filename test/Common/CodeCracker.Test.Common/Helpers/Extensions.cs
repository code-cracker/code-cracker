namespace CodeCracker.Test
{
    public static class Extensions
    {
        public static string WrapInCSharpClass(this string code, string typeName = "TypeName", string usings = "")
        {
            if (!code.StartsWith("\r") || code.StartsWith("\n"))
            {
                code = "        " + code;
            }

            return $@"
using System;{usings}

namespace ConsoleApplication1
{{
    class {typeName}
    {{
{code}
    }}
}}";
        }

        public static string WrapInCSharpMethod(this string code, bool isAsync = false, string typeName = "TypeName", string usings = "")
        {
            if (!code.StartsWith("\r") || code.StartsWith("\n"))
            {
                code = "            " + code;
            }

            return $@"
using System;{usings}

namespace ConsoleApplication1
{{
    class {typeName}
    {{
        public {(isAsync ? "async " : "")}void Foo()
        {{
{code}
        }}
    }}
}}";
        }

        public static string WrapInVBClass(this string code,
            string typeName = "TypeName",
            string imports = "")
        {
            return $@"
Imports System{imports}
Namespace ConsoleApplication1
    Class {typeName}
        {code}
    End Class
End Namespace";
        }

        public static string WrapInVBMethod(this string code,
            bool isAsync = false,
            string typeName = "TypeName",
            string imports = "")
        {
            return $@"
Imports System{imports}
Namespace ConsoleApplication1
    Class {typeName}
        Public {(isAsync ? "Async " : "")}{(isAsync ? "Function" : "Sub")} Foo(){(isAsync ? " As Task" : "")}
            {code}
            ' VB Requires value to be used or another analyzer is added which breaks the tests
            Console.WriteLine(a)
        End {(isAsync ? "Function" : "Sub")}
    End Class
End Namespace";
        }
    }
}
