using System;
using System.Collections.Generic;

namespace CodeCracker
{
    public static class Extensions
    {
        public static string ToDiagnosticId(this DiagnosticId diagnosticId) => $"CC{(int)diagnosticId :D4}";

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
    }
}
