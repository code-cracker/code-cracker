using System;
using System.Collections.Generic;

namespace CodeCracker.CSharp.Usage.MethodAnalyzers
{
    public class MethodInformation
    {
        public string MethodName { get; set; }
        public string MethodFullDefinition { get; set; }
        public int ArgumentIndex { get; set; }
        public Action<List<object>> MethodAction { get; set; }

        public MethodInformation(string methodName, string methodFullDefinition, Action<List<object>> methodAction, int argumentIndex = 0)
        {
            MethodName = methodName;
            MethodFullDefinition = methodFullDefinition;
            ArgumentIndex = argumentIndex;
            MethodAction = methodAction;
        }
    }
}