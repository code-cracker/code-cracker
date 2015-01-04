using System;
using System.Collections.Generic;

namespace CodeCracker.Usage.PreRunMethodAnalyzers {
    public class PreRunMethodInfo {
        public string MethodName { get; set; }
        public string MethodFullDefinition { get; set; }
        public int ArgumentIndex { get; set; }
        public Action<List<object>> MethodToExecuteForChecking { get; set; }

        public PreRunMethodInfo(string methodName, string methodFullDefinition, int argumentIndex, Action<List<object>> methodToExecuteForChecking) {
            MethodName = methodName;
            MethodFullDefinition = methodFullDefinition;
            ArgumentIndex = argumentIndex;
            MethodToExecuteForChecking = methodToExecuteForChecking;
        }

        public PreRunMethodInfo(string methodFullDefinition, Action<List<object>> methodToExecuteForChecking)
        {
            MethodFullDefinition = methodFullDefinition;
            MethodToExecuteForChecking = methodToExecuteForChecking;
        }
    }
}