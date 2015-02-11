namespace CodeCracker.Test
{
    public static class Extensions
    {
        public static string WrapInCSharpMethod(this string code, bool isAsync = false)
        {
            return $@"
    using System;

    namespace ConsoleApplication1
    {{
        class TypeName
        {{
            public {(isAsync ? "async " : "")}void Foo()
            {{
                {code}
            }}
        }}
    }}";
        }
        public static string WrapInVBMethod(this string code, bool isAsync = false)
        {
            return $@"
Imports System
Namespace ConsoleApplication1
    Class TypeName
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
