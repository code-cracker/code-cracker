using System;
using System.Collections.Generic;

namespace CodeCracker
{
    public static class Extensions
    {
        public static string ToDiagnosticId(this DiagnosticId diagnosticId) => $"CC{(int)diagnosticId:D4}";

        //extracted from http://source.roslyn.io/#BoundTreeGenerator/BoundNodeClassWriter.cs,1dcced07beac9209,references
        private static string[] csharpKeywords = new string[] 
        {
                "bool",
                "byte",
                "sbyte",
                "short",
                "ushort",
                "int",
                "uint",
                "long",
                "ulong",
                "double",
                "float",
                "decimal",
                "string",
                "char",
                "object",
                "typeof",
                "sizeof",
                "null",
                "true",
                "false",
                "if",
                "else",
                "while",
                "for",
                "foreach",
                "do",
                "switch",
                "case",
                "default",
                "lock",
                "try",
                "throw",
                "catch",
                "finally",
                "goto",
                "break",
                "continue",
                "return",
                "public",
                "private",
                "internal",
                "protected",
                "static",
                "readonly",
                "sealed",
                "const",
                "new",
                "override",
                "abstract",
                "virtual",
                "partial",
                "ref",
                "out",
                "in",
                "where",
                "params",
                "this",
                "base",
                "namespace",
                "using",
                "class",
                "struct",
                "interface",
                "delegate",
                "checked",
                "get",
                "set",
                "add",
                "remove",
                "operator",
                "implicit",
                "explicit",
                "fixed",
                "extern",
                "event",
                "enum",
                "unsafe"
        };

        public static IDictionary<K, V> AddRange<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> newValues)
        {
            if (dictionary == null || newValues == null) return dictionary;
            foreach (var kv in newValues) dictionary.Add(kv);
            return dictionary;
        }

        public static bool EndsWithAny(this string text, params string[] values) =>
            text.EndsWithAny(StringComparison.CurrentCulture, values);

        public static bool EndsWithAny(this string text, StringComparison comparisonType, params string[] values)
        {
            foreach (var value in values)
                if (text.EndsWith(value, comparisonType)) return true;
            return false;
        }

        public static string ToLowerCaseFirstLetter(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (text.Length == 1) return text.ToLower();
            return char.ToLowerInvariant(text[0]) + text.Substring(1);
        }

        public static bool IsCSharpKeyword(this string name)
        {
            return (Array.IndexOf(csharpKeywords, name) > -1);
        }
    }
}
