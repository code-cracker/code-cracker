// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

//this works, but is specific to a method, so it would be too much work to use:
//1: [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "CC0061:TaskNameAsyncAnalyzer", Justification = "No need for tests to have async sufix.", Scope = "member", Target = "~M:CodeCracker.Test.Design.CopyEventToVariableBeforeFireTests.IgnoreMemberAccess")]
//this should work but does not we are keeping it here only because it should work in the future
//right now we are using a .ruleset to exclude CC0061 from the test project
//2: [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "CC0061", Scope = "namespace", Target = "CodeCracker.Test.Design", Justification = "No need for tests to have async sufix.")]