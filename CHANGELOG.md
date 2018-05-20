# Change Log

## [v1.1.0](https://github.com/code-cracker/code-cracker/tree/v1.1.0) (2018-05-20)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.3...v1.1.0)

**Implemented enhancements:**

- Create a reusable FixAllProvider based on IntroduceFieldFromConstructorCodeFixProviderAll [\#910](https://github.com/code-cracker/code-cracker/issues/910)
- Enable CodeCracker to work with .NET Core [\#871](https://github.com/code-cracker/code-cracker/issues/871)
- CC0068 \(Remove private method\): ShouldSerializeXXX\(\) and ResetXXX\(\) should not trigger the message [\#762](https://github.com/code-cracker/code-cracker/issues/762)
- Create PropertyChangedEventArgs statically [\#42](https://github.com/code-cracker/code-cracker/issues/42)

- CC0013 Check for applicability as argument [\#522](https://github.com/code-cracker/code-cracker/issues/522)
- Make readonly \(for complex value types\) [\#808](https://github.com/code-cracker/code-cracker/issues/808)
- CC0120: Suggest default for switch statements \(C\#\) [\#780](https://github.com/code-cracker/code-cracker/issues/780)
- Remove Unnecessary ToString in String Concatenation [\#753](https://github.com/code-cracker/code-cracker/issues/753)
- Check consistency of optional parameter default value [\#575](https://github.com/code-cracker/code-cracker/issues/575)
- Make accessibility consistent \(code fix for CS0050 to CS0061\) [\#381](https://github.com/code-cracker/code-cracker/issues/381)
- Prefer "Any" to "Count\(\) \> 0" [\#490](https://github.com/code-cracker/code-cracker/issues/490)
- Prefer "Count" to "Count\(\)" [\#489](https://github.com/code-cracker/code-cracker/issues/489)
- Extract Class to a New File [\#382](https://github.com/code-cracker/code-cracker/issues/382)
- Seal member if possible [\#372](https://github.com/code-cracker/code-cracker/issues/372)
- Remove virtual modifier if possible [\#371](https://github.com/code-cracker/code-cracker/issues/371)
- Remove async and return task directly [\#151](https://github.com/code-cracker/code-cracker/issues/151)
- Change from as operator to direct cast or the opposite [\#65](https://github.com/code-cracker/code-cracker/issues/65)
- Convert loop to linq expression [\#22](https://github.com/code-cracker/code-cracker/issues/22)

**Fixed bugs:**

- CC0061 shouldn't pop for async Main [\#958](https://github.com/code-cracker/code-cracker/issues/958)
- Bug: CC0061: Implementing interface using async keyword should not raise a diagnostic [\#936](https://github.com/code-cracker/code-cracker/issues/936)
- Bug CC0031 UseInvokeMethodToFireEventAnalyzer false positive in constructor [\#926](https://github.com/code-cracker/code-cracker/issues/926)
- BUG: CC0014 Casting to interface or implicit casts for the ternary operator are fixed wrong [\#911](https://github.com/code-cracker/code-cracker/issues/911)
- TernaryOperatorWithReturnCodeFixProvider NullReferenceException [\#906](https://github.com/code-cracker/code-cracker/issues/906)
- BUG: CC0022 failed to show fix with null coalesce operator [\#870](https://github.com/code-cracker/code-cracker/issues/870)
- BUG: CC0118 - Unnecessary '.ToString\(\)' call in string concatenation [\#866](https://github.com/code-cracker/code-cracker/issues/866)

**Closed issues:**

- Support Hacktoberfest event adding hacktoberfest tag. [\#949](https://github.com/code-cracker/code-cracker/issues/949)
- Using Extension & NuGet package together causes VS2017 to crash [\#944](https://github.com/code-cracker/code-cracker/issues/944)
- False postives and NullReferenceException when using eventhandler in code behind for UWP apps. [\#916](https://github.com/code-cracker/code-cracker/issues/916)
- Replace getter only properties with backing readonly field with getter-only auto-property [\#881](https://github.com/code-cracker/code-cracker/issues/881)

## [v1.0.3](https://github.com/code-cracker/code-cracker/tree/v1.0.3) (2017-03-20)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.2...v1.0.3)

**Fixed bugs:**

- BUG: CC0022 DisposableVariableNotDisposedAnalyzer: False positive for expression bodied members [\#880](https://github.com/code-cracker/code-cracker/issues/880)
- BUG: CC0022 DisposableVariableNotDisposedAnalyzer: False positive in iterator methods [\#877](https://github.com/code-cracker/code-cracker/issues/877)

## [v1.0.2](https://github.com/code-cracker/code-cracker/tree/v1.0.2) (2017-03-12)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.1...v1.0.2)

**Implemented enhancements:**

- VS 2017RC Support [\#856](https://github.com/code-cracker/code-cracker/issues/856)

**Fixed bugs:**

-  CC0057 UnusedParametersAnalyzer should not be triggered on virtual methods [\#872](https://github.com/code-cracker/code-cracker/issues/872)
- BUG: CC0060 - detected for nested struct in abstract class [\#867](https://github.com/code-cracker/code-cracker/issues/867)
- Bug on CC0120 for the fix when there is a conversion [\#859](https://github.com/code-cracker/code-cracker/issues/859)
- BUG: CC0052 \(Make readonly\) sometimes detects complex value types [\#854](https://github.com/code-cracker/code-cracker/issues/854)
- BUG: CC0052 \(Make readonly\) does not work with lambda expressions and initialized variables [\#853](https://github.com/code-cracker/code-cracker/issues/853)
- "Disposable Field Not Disposed" rule does not recognize null propagation [\#848](https://github.com/code-cracker/code-cracker/issues/848)
- CC0082: ComputeExpressionCodeFixProvider crashs [\#841](https://github.com/code-cracker/code-cracker/issues/841)
- CC0030: Bad grammar in message [\#838](https://github.com/code-cracker/code-cracker/issues/838)
- CC0008: Don't suggest for dynamic objects [\#837](https://github.com/code-cracker/code-cracker/issues/837)
- Bug: Should not use Async methods in analyzers \(CC0029\) [\#821](https://github.com/code-cracker/code-cracker/issues/821)
- BUG: CC0014 converts if y then x += 1 else x =1 to x +=\(if\(y, 1, 1\) [\#798](https://github.com/code-cracker/code-cracker/issues/798)

## [v1.0.1](https://github.com/code-cracker/code-cracker/tree/v1.0.1) (2016-09-06)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0...v1.0.1)

**Implemented enhancements:**

- developmentDependency not added when used with SonarLint [\#829](https://github.com/code-cracker/code-cracker/issues/829)
- Auto generated files detection [\#773](https://github.com/code-cracker/code-cracker/issues/773)

**Fixed bugs:**

- Bug: "UseStaticRegexIsMatchAnalyzer" causes an exception \(CC0081\) [\#822](https://github.com/code-cracker/code-cracker/issues/822)
- CC0006 could break code when changing to foreach [\#814](https://github.com/code-cracker/code-cracker/issues/814)
- BUG: Make readonly \(CC0052\) is incorrectly raised if constructor shows up after member that uses the field [\#812](https://github.com/code-cracker/code-cracker/issues/812)
- Bug: ArgumentNullException on CallExtensionMethodAsExtensionAnalyzer \(CC0026\) [\#810](https://github.com/code-cracker/code-cracker/issues/810)
- BUG: GC.SuppressFinalize with arrow methods \(CC0029\) [\#809](https://github.com/code-cracker/code-cracker/issues/809)
- Bug: CC0039 False positive when concatenating to loop variable propery/field \(StringBuilderInLoop\) [\#797](https://github.com/code-cracker/code-cracker/issues/797)
- Bug: CC0033 appears again after adding 'this' keyword [\#795](https://github.com/code-cracker/code-cracker/issues/795)
- CC0061: Implementing interface using async pattern [\#793](https://github.com/code-cracker/code-cracker/issues/793)
- CC0052 Make field readonly does not take ref out into consideration. [\#788](https://github.com/code-cracker/code-cracker/issues/788)
- BUG: CallExtensionMethodAsExtensionAnalyzer threw exception when project language was C\# 5.0 [\#781](https://github.com/code-cracker/code-cracker/issues/781)
- Bug: UseInvokeMethodToFireEventAnalyzer \(CC0031\) should not raise a diagnostic when already checked for null with a ternary [\#779](https://github.com/code-cracker/code-cracker/issues/779)
- BUG: GC.SuppressFinalize within any block \(CC0029\) [\#776](https://github.com/code-cracker/code-cracker/issues/776)
- BUG: CC0052 \(Make readonly\) should not be applied to complex value types [\#775](https://github.com/code-cracker/code-cracker/issues/775)
- BUG: CC0030 should not try to make a pointer const [\#774](https://github.com/code-cracker/code-cracker/issues/774)

**Closed issues:**

- Verify impact of upgrading to Roslyn 1.1 [\#770](https://github.com/code-cracker/code-cracker/issues/770)

## [v1.0.0](https://github.com/code-cracker/code-cracker/tree/v1.0.0) (2016-04-03)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc6...v1.0.0)

**Implemented enhancements:**

- CC0016 and CC0031 are doing similar work [\#662](https://github.com/code-cracker/code-cracker/issues/662)
- Update XmlDocumentation to raise 2 different diagnostic ids [\#488](https://github.com/code-cracker/code-cracker/issues/488)

**Fixed bugs:**

- BUG: using null propagation foils the analysis for dispose object \(CC0022\) [\#761](https://github.com/code-cracker/code-cracker/issues/761)
- CC0011 incorrect when Where predicate uses index parameter [\#752](https://github.com/code-cracker/code-cracker/issues/752)
- BUG: Change to ternary fails with primitive numeric types \(CC0013 and CC0014\) [\#748](https://github.com/code-cracker/code-cracker/issues/748)
- CC0014 Use ternary tries to DirectCast integer to double [\#745](https://github.com/code-cracker/code-cracker/issues/745)
- BUG: CC0068 'Method is not used' should not apply to methods that have the ContractInvariantMethod attribute [\#744](https://github.com/code-cracker/code-cracker/issues/744)
- BUG: CC0057 UnusedParametersAnalyzer should not be triggered with verbatim identifier \(prefixed with @\) [\#741](https://github.com/code-cracker/code-cracker/issues/741)
- Code fix for CC0075 could break the logic of the code [\#740](https://github.com/code-cracker/code-cracker/issues/740)
- BUG: CC0016 "CopyEventToVariableBeforeFireCodeFixProvider" crashes [\#735](https://github.com/code-cracker/code-cracker/issues/735)
- CC0057 UnusedParametersAnalyzer should not be triggered by DllImport [\#733](https://github.com/code-cracker/code-cracker/issues/733)
- BUG: CC0013 \(Make Ternary\) doesn't handle comments correctly [\#725](https://github.com/code-cracker/code-cracker/issues/725)
- BUG: CC0014: You can use a ternary operator turns a+=b into a=a+b [\#724](https://github.com/code-cracker/code-cracker/issues/724)
- Bug in introduce field from constructor [\#721](https://github.com/code-cracker/code-cracker/issues/721)
- BUG: CC0090 \(xmldoc\) is raised on generated files [\#720](https://github.com/code-cracker/code-cracker/issues/720)
- BUG: CC0008 code fix removes too many lines when constructor already has initializer [\#717](https://github.com/code-cracker/code-cracker/issues/717)
- BUG: CC0067 must not fire when parameter Func\<T\> is called in constructor. [\#712](https://github.com/code-cracker/code-cracker/issues/712)
- BUG: CC0033 \(DisposableFieldNotDisposed\) Should ignore static field [\#710](https://github.com/code-cracker/code-cracker/issues/710)
- CC0017 virtual props can create infinite loops [\#702](https://github.com/code-cracker/code-cracker/issues/702)
- CC0057: extern method parameter unused [\#701](https://github.com/code-cracker/code-cracker/issues/701)
- BUG: CC0052 \(readonly\) flags field that is used in variable initializer, which then gives compile error [\#700](https://github.com/code-cracker/code-cracker/issues/700)
- BUG: CC0017 \(create auto property\) removing modifiers \(static, virtual, etc\) [\#699](https://github.com/code-cracker/code-cracker/issues/699)
- BUG: CC0006 \(foreach\) edge case for non-writable struct field [\#698](https://github.com/code-cracker/code-cracker/issues/698)
- BUG: CC00091 Method `GetEnumerator` should never be made static [\#696](https://github.com/code-cracker/code-cracker/issues/696)
- Bug: CC0048 nested interpolations doesn't fix in one iteration [\#690](https://github.com/code-cracker/code-cracker/issues/690)
- BUG: CC0091 WPF event cannot be static [\#639](https://github.com/code-cracker/code-cracker/issues/639)
- DisposableVariableNotDisposedAnalyzer \(CC0022\) should not raise a diagnostic when returning a type that contains the disposable [\#465](https://github.com/code-cracker/code-cracker/issues/465)

## [v1.0.0-rc6](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc6) (2016-02-01)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc5...v1.0.0-rc6)

**Implemented enhancements:**

- CC0031 must not fire when Null-check is outside of a try block [\#672](https://github.com/code-cracker/code-cracker/issues/672)
- Add exceptions to CC0068 depending on the method attribute [\#526](https://github.com/code-cracker/code-cracker/issues/526)

**Fixed bugs:**

- BUG: CC0048 \(string interpolation\) when string is split into multiple lines [\#689](https://github.com/code-cracker/code-cracker/issues/689)
- CC0017 wrong change with explicitly implemented properties, but can be fixed [\#687](https://github.com/code-cracker/code-cracker/issues/687)
- CC0006 should be checking if for-looped array is enumerable by foreach [\#684](https://github.com/code-cracker/code-cracker/issues/684)
- BUG: CC0006 wrongly tries to convert for into foreach when there would be an assignment to the iteration variable [\#683](https://github.com/code-cracker/code-cracker/issues/683)
- CC0091 MakeMethodStaticCodeFixProvider crashing [\#682](https://github.com/code-cracker/code-cracker/issues/682)
- CC0091 wrongly tries to make static methods in structs [\#681](https://github.com/code-cracker/code-cracker/issues/681)
- BUG: CC0091 \(make static\) changes references incorrectly when used as a method group and in conjunction with another method invocation [\#680](https://github.com/code-cracker/code-cracker/issues/680)
- CC0091 on method Foo does not replace occurences like this.Foo used in delegates [\#677](https://github.com/code-cracker/code-cracker/issues/677)
- ObjectInitializerCodeFixProvider crashes [\#676](https://github.com/code-cracker/code-cracker/issues/676)
- Bug CC0084 should not generate string empty in parameter list in constructor or method [\#669](https://github.com/code-cracker/code-cracker/issues/669)
- CC0022 when theer is ternary operator in using [\#665](https://github.com/code-cracker/code-cracker/issues/665)
- CC0031 should not copy the variable with readonly members [\#663](https://github.com/code-cracker/code-cracker/issues/663)
- BUG on CC0047 \(PropertyPrivateSet\) suggests to make a property set private when the property is already private [\#658](https://github.com/code-cracker/code-cracker/issues/658)
- With more than one constructor, IntroduceFieldFromConstructorCodeFixProvider can replace wrong constructor [\#650](https://github.com/code-cracker/code-cracker/issues/650)
- BUG on CC0057 \(unused parameter\) with named parameters [\#649](https://github.com/code-cracker/code-cracker/issues/649)
- CC0057: Should not try to remove parameters meant to fulfill a delegate contract [\#646](https://github.com/code-cracker/code-cracker/issues/646)
- CC0016 should not copy the variable with readonly members [\#645](https://github.com/code-cracker/code-cracker/issues/645)
- BUG on CC0091 \(MakeMethodStatic\) when a reference to method is used as a method group [\#644](https://github.com/code-cracker/code-cracker/issues/644)
- BUG on CC0043 and CC0092 \(ChangeAnyToAll\) when invocation is negated [\#642](https://github.com/code-cracker/code-cracker/issues/642)
- BUG on CC0043 and CC0092 \(ChangeAnyToAll\) when invocation is in an expression bodied member [\#641](https://github.com/code-cracker/code-cracker/issues/641)
- AD0001 Crash [\#638](https://github.com/code-cracker/code-cracker/issues/638)
- BUG on CC0022 \(disposable not disposed\) fix when there is method chaining [\#630](https://github.com/code-cracker/code-cracker/issues/630)
- New word 'Foramt' in NameOfSymbolDisplayForamt [\#627](https://github.com/code-cracker/code-cracker/issues/627)
- CC0003 is shown even for catches which end with throw [\#626](https://github.com/code-cracker/code-cracker/issues/626)
- CC0001 is shown for variables with type dynamic and object [\#625](https://github.com/code-cracker/code-cracker/issues/625)
- CC0090 when using /// \<inheritdoc/\> [\#624](https://github.com/code-cracker/code-cracker/issues/624)
- Bug: CC0017 not raised when using this [\#623](https://github.com/code-cracker/code-cracker/issues/623)
- CC0052 false positives [\#621](https://github.com/code-cracker/code-cracker/issues/621)
- Bug on CC0029 \(Call GC.SuppressFinalize on dispose\) with expression based methods [\#618](https://github.com/code-cracker/code-cracker/issues/618)
- Removing Option Parameter causes crash [\#617](https://github.com/code-cracker/code-cracker/issues/617)
- BUG on CC0043 and CC0092 \(ChangeAnyToAll\) when invocation has elvis operator [\#613](https://github.com/code-cracker/code-cracker/issues/613)
- When the dispose pattern is used do not raise CC0033 [\#603](https://github.com/code-cracker/code-cracker/issues/603)
- Suggested Nameof fix incorrectly suggests wrong type [\#594](https://github.com/code-cracker/code-cracker/issues/594)

## [v1.0.0-rc5](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc5) (2015-12-03)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc4...v1.0.0-rc5)

**Implemented enhancements:**

- CC0091 Know APIs [\#451](https://github.com/code-cracker/code-cracker/issues/451)
- Change CC0001 so that it does not apply to primitives [\#407](https://github.com/code-cracker/code-cracker/issues/407)
- Update all existing code fixes that are doing too much work on RegisterCodeFixesAsync \(VB\) [\#348](https://github.com/code-cracker/code-cracker/issues/348)

**Fixed bugs:**

- BUG: UseInvokeMethodToFireEventAnalyzer throwing when method body is null [\#611](https://github.com/code-cracker/code-cracker/issues/611)
- BUG: ReadonlyFieldAnalyzer is crashing Visual Studio 2015 Update 1 because of reporting diagnostic at unexpected locations [\#610](https://github.com/code-cracker/code-cracker/issues/610)
- NoPrivateReadonlyFieldAnalyzer \(0074\) is missing the Generated file check [\#609](https://github.com/code-cracker/code-cracker/issues/609)
- BUG on CC0049 [\#597](https://github.com/code-cracker/code-cracker/issues/597)
- BUG: DisposablesShouldCallSuppressFinalize should use full name when System is not imported [\#590](https://github.com/code-cracker/code-cracker/issues/590)
- Remove empty Catch Block too aggressive [\#587](https://github.com/code-cracker/code-cracker/issues/587)
- BUG: CC0056 \(StringFormatAnalyzer\) raised incorrectly [\#585](https://github.com/code-cracker/code-cracker/issues/585)
- BUG on CC0022 \(disposable not disposed\) when a comment is present [\#577](https://github.com/code-cracker/code-cracker/issues/577)
- BUG on CC0057 \(unused parameter\) with extension methods [\#576](https://github.com/code-cracker/code-cracker/issues/576)
- IP Parsing fails for variables [\#571](https://github.com/code-cracker/code-cracker/issues/571)
- CC0057 mistaken 'Parameter is not used' warning [\#562](https://github.com/code-cracker/code-cracker/issues/562)
- Code fix error in CC0013 TernaryOperatorAnalyzer \(return\) [\#552](https://github.com/code-cracker/code-cracker/issues/552)
- Code fix in CC0014 TernaryOperatorAnalyzer remove comments [\#551](https://github.com/code-cracker/code-cracker/issues/551)
- Code fix error in CC0014 TernaryOperatorAnalyzer \(assignment\)  [\#550](https://github.com/code-cracker/code-cracker/issues/550)
- Bug with CC0009 when using the created object in the initialization [\#525](https://github.com/code-cracker/code-cracker/issues/525)
- CC0013 Should check for a common type and cast is necessary. [\#521](https://github.com/code-cracker/code-cracker/issues/521)

## [v1.0.0-rc4](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc4) (2015-11-02)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc3...v1.0.0-rc4)

**Implemented enhancements:**

- NameOf Analyzer is too noisy [\#518](https://github.com/code-cracker/code-cracker/issues/518)

**Fixed bugs:**

- SwitchToAutoPropCodeFixProvider \(CC0017\) does not keep XML comment trivia [\#548](https://github.com/code-cracker/code-cracker/issues/548)
- CallExtensionMethodAsExtension \(CC0026\) throws NullReferenceException on expression bodied method statement [\#547](https://github.com/code-cracker/code-cracker/issues/547)
- CC0022 DI container delegate causes error [\#545](https://github.com/code-cracker/code-cracker/issues/545)
- BUG on ReadonlyFieldAnalyzer \(CC0052\) with assignment in Func\<T\> in constructor [\#544](https://github.com/code-cracker/code-cracker/issues/544)
- UnusedParametersCodeFixProvider crashing when removing params [\#539](https://github.com/code-cracker/code-cracker/issues/539)
- CC0001 Is raised on multiple variable declarations. [\#537](https://github.com/code-cracker/code-cracker/issues/537)
- Bug: Use ?.Invoke operator and method to fire 'configuration' event, though configuration can't be null [\#536](https://github.com/code-cracker/code-cracker/issues/536)
- UnusedParametersCodeFixProvider fix all not working \(CC0057\) [\#534](https://github.com/code-cracker/code-cracker/issues/534)
- UnusedParametersCodeFixProvider will crash when it is trying to remove ParamArray [\#533](https://github.com/code-cracker/code-cracker/issues/533)
- CC0017 Change to auto property fix all not working [\#514](https://github.com/code-cracker/code-cracker/issues/514)
- BUG on CC0008 and CC0009 \(ObjectInitializer\) when used with collection [\#501](https://github.com/code-cracker/code-cracker/issues/501)
- 'TernaryOperatorWithAddignmentCodeFixProvider' encountered and error [\#496](https://github.com/code-cracker/code-cracker/issues/496)
- CC0009 eats pragmas and trivia [\#493](https://github.com/code-cracker/code-cracker/issues/493)
- CC0013 \(user ternary\) rule should be more careful with nullable types. \(VB\) [\#468](https://github.com/code-cracker/code-cracker/issues/468)

## [v1.0.0-rc3](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc3) (2015-10-03)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc2...v1.0.0-rc3)

**Implemented enhancements:**

- CC0013 Should be Information instead of Warning [\#520](https://github.com/code-cracker/code-cracker/issues/520)

**Fixed bugs:**

- CC0017 Change to auto property codefix removes multiple variable declaration. [\#512](https://github.com/code-cracker/code-cracker/issues/512)
- CC0017 Change to auto property ignores field assignment. [\#500](https://github.com/code-cracker/code-cracker/issues/500)
- CC047 eats property trivia [\#492](https://github.com/code-cracker/code-cracker/issues/492)
- CC0091 makes static keyword come before trivia [\#486](https://github.com/code-cracker/code-cracker/issues/486)
- Useles suggestion of CC0039 rule  [\#485](https://github.com/code-cracker/code-cracker/issues/485)
- CC0013 \(user ternary\) rule should be more careful with nullable types \(C\#\) [\#480](https://github.com/code-cracker/code-cracker/issues/480)
- CC0091 \(make method static\) Reported Incorrectly for explicitly implemented interface [\#479](https://github.com/code-cracker/code-cracker/issues/479)
- CC0068 \(method not used\) Reported Incorrectly for explicitly implemented interface [\#478](https://github.com/code-cracker/code-cracker/issues/478)
- Add additional checks for determining if generated code [\#476](https://github.com/code-cracker/code-cracker/issues/476)
- CC0013 & CC0014 for VB is reported in generated code [\#472](https://github.com/code-cracker/code-cracker/issues/472)
- CC0068 Reported Incorrectly for VB Event Handlers [\#469](https://github.com/code-cracker/code-cracker/issues/469)
- Bug on DisposableVariableNotDisposedAnalyzer \(CC0022\) when returning a disposable [\#466](https://github.com/code-cracker/code-cracker/issues/466)
- Bug on analyzer CC0031 \(UseInvokeMethodToFireEvent\) on parameterized functor [\#397](https://github.com/code-cracker/code-cracker/issues/397)
- BUG: Create code fix for update ReadonlyFieldCodeFixProvider so that the code does not break on public fields [\#293](https://github.com/code-cracker/code-cracker/issues/293)

## [v1.0.0-rc2](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc2) (2015-08-19)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-rc1...v1.0.0-rc2)

**Implemented enhancements:**

- Update VB Allow Members Ordering to work with Modules [\#440](https://github.com/code-cracker/code-cracker/issues/440)
- Provide an equivalence key on all code fix providers [\#417](https://github.com/code-cracker/code-cracker/issues/417)
- Unit test methods raises CC0091 - "Make \<xxxx\> method static" [\#404](https://github.com/code-cracker/code-cracker/issues/404)
- Validate color from System.Drawing.ColorTranslator.FromHtml [\#1](https://github.com/code-cracker/code-cracker/issues/1)

**Fixed bugs:**

- Erroneous CC0039 message [\#461](https://github.com/code-cracker/code-cracker/issues/461)
- CC0029 DisposablesShouldCallSuppressFinalizeAnalyzer throws InvalidCastException [\#452](https://github.com/code-cracker/code-cracker/issues/452)
- Doc comments for parameters in wrong order \(XmlDocumentationCreateMissingParametersCodeFixProvider\) [\#437](https://github.com/code-cracker/code-cracker/issues/437)
- CC0022 disposable intantiation on constructor call [\#432](https://github.com/code-cracker/code-cracker/issues/432)
- CC0017 Change to auto property has Incorrect description [\#429](https://github.com/code-cracker/code-cracker/issues/429)
- BUG in CC0061 when the method is a override from a base class [\#424](https://github.com/code-cracker/code-cracker/issues/424)
- CC0021: nameof\(x\) suggested before x is declared \(VB\) [\#420](https://github.com/code-cracker/code-cracker/issues/420)
- Bug: RemoveUnusedVariablesCodeFixProvider is throwing on CatchDeclarations [\#419](https://github.com/code-cracker/code-cracker/issues/419)
- CC064 invalid error [\#418](https://github.com/code-cracker/code-cracker/issues/418)
- BUG: XmlDocumentationCreateMissingParametersCodeFixProvider throws when there are only remarks [\#412](https://github.com/code-cracker/code-cracker/issues/412)
- Bug: DisposableVariableNotDisposedCodeFixProvider throws when object creation is being passed as an argument \(CC0022\) [\#409](https://github.com/code-cracker/code-cracker/issues/409)
- CC0021: nameof\(x\) suggested before x is declared [\#408](https://github.com/code-cracker/code-cracker/issues/408)

## [v1.0.0-rc1](https://github.com/code-cracker/code-cracker/tree/v1.0.0-rc1) (2015-07-23)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-beta1...v1.0.0-rc1)

**Implemented enhancements:**

- Verify if xml docs have the correct parameters [\#357](https://github.com/code-cracker/code-cracker/issues/357)

**Fixed bugs:**

- BUG: CopyEventToVariableBeforeFireCodeFixProvider throwing [\#411](https://github.com/code-cracker/code-cracker/issues/411)
- Incorrect spacing in string interpolation \(CC0048\) [\#410](https://github.com/code-cracker/code-cracker/issues/410)

## [v1.0.0-beta1](https://github.com/code-cracker/code-cracker/tree/v1.0.0-beta1) (2015-07-04)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha6...v1.0.0-beta1)

**Implemented enhancements:**

- Make method non async \(fix for CS1998\) [\#393](https://github.com/code-cracker/code-cracker/issues/393)
- Using the refactoring for CC0008 will result in a warning CC0015 [\#383](https://github.com/code-cracker/code-cracker/issues/383)
- Add string interpolation to Console.WriteLine [\#380](https://github.com/code-cracker/code-cracker/issues/380)
- Make method static if possible [\#364](https://github.com/code-cracker/code-cracker/issues/364)
- Update all existing code fixes that are doing too much work on RegisterCodeFixesAsync \(C\#\) [\#347](https://github.com/code-cracker/code-cracker/issues/347)
- Make accessibility consistent \(code fix for CS0051\) [\#321](https://github.com/code-cracker/code-cracker/issues/321)
- Expand CC0006 \(change for into a foreach\) [\#318](https://github.com/code-cracker/code-cracker/issues/318)
- Convert a "Not Any" query for a condition to a "All" query for the inverted condition [\#31](https://github.com/code-cracker/code-cracker/issues/31)

**Fixed bugs:**

- Ignore Disposables created on return statements [\#399](https://github.com/code-cracker/code-cracker/issues/399)
- Null Reference Exception on CodeCracker.CSharp.Usage.VirtualMethodOnConstructorAnalyzer [\#398](https://github.com/code-cracker/code-cracker/issues/398)
- Bug on ObjectInitializerCodeFixProvider when constructor already has initialization [\#396](https://github.com/code-cracker/code-cracker/issues/396)
- Async method can't be used with RegisterXXXXAction. [\#375](https://github.com/code-cracker/code-cracker/issues/375)
- Bug on CC0068 \(RemovePrivateMethodNeverUsed\) with Main [\#368](https://github.com/code-cracker/code-cracker/issues/368)
- Bug in CC0008 when used together with null coalescing operator ?? [\#366](https://github.com/code-cracker/code-cracker/issues/366)
- Bug: nameof analyzer \(CC0021\) being raised on self referencing initialization statement [\#359](https://github.com/code-cracker/code-cracker/issues/359)

## [v1.0.0-alpha6](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha6) (2015-06-01)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha5...v1.0.0-alpha6)

**Implemented enhancements:**

- Remove redundant else [\#355](https://github.com/code-cracker/code-cracker/issues/355)
- Use "" instead of String.Empty [\#354](https://github.com/code-cracker/code-cracker/issues/354)
- Port changes from CC0021 \(nameof\) to VB [\#341](https://github.com/code-cracker/code-cracker/issues/341)
- Incorrect string.Format usage [\#335](https://github.com/code-cracker/code-cracker/issues/335)
- Expand NameOf Analyzer to work with any identifier in scope [\#263](https://github.com/code-cracker/code-cracker/issues/263)
- Test NameOfCodeFixProvider with keyword [\#198](https://github.com/code-cracker/code-cracker/issues/198)
- Use String.Empty instead "" [\#120](https://github.com/code-cracker/code-cracker/issues/120)
- Use auto property when possible [\#12](https://github.com/code-cracker/code-cracker/issues/12)
- Supress assignment of default value to field/property declarations [\#9](https://github.com/code-cracker/code-cracker/issues/9)

**Fixed bugs:**

- Bug on CallExtensionMethodAsExtensionAnalyzer \(CC0026\) throwing InvalidOperationException [\#345](https://github.com/code-cracker/code-cracker/issues/345)
- Bug on CC0068 \(RemovePrivateMethodNeverUsed\) with generic methods [\#343](https://github.com/code-cracker/code-cracker/issues/343)
- BUG: CC0056 Incorrectly classifying format strings [\#333](https://github.com/code-cracker/code-cracker/issues/333)
- CC0056 Incorrectly classifying format strings. [\#330](https://github.com/code-cracker/code-cracker/issues/330)

## [v1.0.0-alpha5](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha5) (2015-04-26)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha4...v1.0.0-alpha5)

**Implemented enhancements:**

- Create infrastructure for testing multi culture values [\#313](https://github.com/code-cracker/code-cracker/issues/313)
- View lines covered by tests in coverwalls [\#304](https://github.com/code-cracker/code-cracker/issues/304)
- Regex performance: use static Regex.IsMatch [\#297](https://github.com/code-cracker/code-cracker/issues/297)
- Don't run CodeCracker on generated code [\#260](https://github.com/code-cracker/code-cracker/issues/260)
- Virtual method call in constructor [\#203](https://github.com/code-cracker/code-cracker/issues/203)
- Merge nested if statements [\#131](https://github.com/code-cracker/code-cracker/issues/131)
- Split 'if' with '&&' condition into nested 'if'-statements [\#130](https://github.com/code-cracker/code-cracker/issues/130)
- Convert numeric literal from decimal to hex and hex to decimal [\#119](https://github.com/code-cracker/code-cracker/issues/119)
- Compute value of an expression and replaces it whenever it's possible  [\#117](https://github.com/code-cracker/code-cracker/issues/117)
- Check arguments in String.Format [\#116](https://github.com/code-cracker/code-cracker/issues/116)
- ArgumentExceptionAnalyzer ignores several code constructs [\#112](https://github.com/code-cracker/code-cracker/issues/112)
- Remove unused variables [\#23](https://github.com/code-cracker/code-cracker/issues/23)

**Fixed bugs:**

- Bug in CC0008 analyser for variable access. [\#324](https://github.com/code-cracker/code-cracker/issues/324)
- Bug: CC0022 being raised on using when not assigned [\#319](https://github.com/code-cracker/code-cracker/issues/319)
- Visual studio crash on initializer refactoring \(CC0009\) [\#315](https://github.com/code-cracker/code-cracker/issues/315)
- NameOf produces wrong results/ [\#309](https://github.com/code-cracker/code-cracker/issues/309)
- Bug on Unused parameter \(CC0057\) not checking for assignment [\#291](https://github.com/code-cracker/code-cracker/issues/291)
- BUG: Method not used \(CC0068\) fails to check partial classes [\#290](https://github.com/code-cracker/code-cracker/issues/290)
- Bug on CC0026 \(use extension method\) where selected overload would change [\#262](https://github.com/code-cracker/code-cracker/issues/262)
- Null Reference BUG on ConvertToExpressionBodiedMemberAnalyzer \(CC0038\) [\#192](https://github.com/code-cracker/code-cracker/issues/192)
- CC0029 should not be reported for some types with private constructors [\#110](https://github.com/code-cracker/code-cracker/issues/110)
- CC0029 should not be reported for some sealed types [\#109](https://github.com/code-cracker/code-cracker/issues/109)
- CC0029 should not be reported for structs [\#108](https://github.com/code-cracker/code-cracker/issues/108)
- CC0029 should not be reported for Dispose\(bool\) [\#107](https://github.com/code-cracker/code-cracker/issues/107)
- Fix analysis for CC0029 \(DisposablesShouldCallSuppressFinalizeAnalyzer\) [\#95](https://github.com/code-cracker/code-cracker/issues/95)

## [v1.0.0-alpha4](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha4) (2015-03-04)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha3...v1.0.0-alpha4)

**Implemented enhancements:**

- Update to Roslyn RC1 and VS 2015 CTP 6 [\#288](https://github.com/code-cracker/code-cracker/issues/288)
- Review AllowMembersOrderingCodeFixProvider.Base to avoid running analyzis on ComputeFixesAsync method [\#273](https://github.com/code-cracker/code-cracker/issues/273)
- Add braces to switch case [\#252](https://github.com/code-cracker/code-cracker/issues/252)
- Unify DiagnosticId class on a separate assembly [\#248](https://github.com/code-cracker/code-cracker/issues/248)
- If method does not return a Task it shouldn't end with "Async" [\#245](https://github.com/code-cracker/code-cracker/issues/245)
- Use enum for diagnostic ids \(VB\) [\#244](https://github.com/code-cracker/code-cracker/issues/244)
- Introduce field from constructor [\#241](https://github.com/code-cracker/code-cracker/issues/241)
- Remove private method is never used in a class [\#204](https://github.com/code-cracker/code-cracker/issues/204)
- Detect read-only not private fields and fix adding the "readonly" modifier [\#177](https://github.com/code-cracker/code-cracker/issues/177)
- Convert Lambda to Method Group whenever it's possible [\#49](https://github.com/code-cracker/code-cracker/issues/49)
- Review Rules ID and Descriptions  [\#41](https://github.com/code-cracker/code-cracker/issues/41)
- Remove unreachable code [\#21](https://github.com/code-cracker/code-cracker/issues/21)

**Fixed bugs:**

- RemovePrivateMethodNeverUsedAnalyzer \(CC0068\) throwing cast exception [\#276](https://github.com/code-cracker/code-cracker/issues/276)
- Bug on CC0071 \(introduce field\) for fix all [\#267](https://github.com/code-cracker/code-cracker/issues/267)
- BUG on CC0071 \(introduce field\), name clash [\#266](https://github.com/code-cracker/code-cracker/issues/266)
- BUG on CC0021 \(NameOf\) when using attribute [\#258](https://github.com/code-cracker/code-cracker/issues/258)
- CC0031 Formatting bug on comments [\#257](https://github.com/code-cracker/code-cracker/issues/257)
- Bug on CC0065, entire summary removed. [\#256](https://github.com/code-cracker/code-cracker/issues/256)
- CC0022 Batch Fixer does not work in edge cases [\#253](https://github.com/code-cracker/code-cracker/issues/253)
- BUG on CC0057 \(UnusedParameter\) on constructor that passes argument to base [\#251](https://github.com/code-cracker/code-cracker/issues/251)
- Bug in string interpolation \(CC0048\) when Insert uses a ternary operator [\#249](https://github.com/code-cracker/code-cracker/issues/249)

## [v1.0.0-alpha3](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha3) (2015-02-01)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha2...v1.0.0-alpha3)

**Implemented enhancements:**

- Use ConfigureAwait\(false\) on awaited task [\#235](https://github.com/code-cracker/code-cracker/issues/235)
- CTP5 nuget "The assembly \<...\> does not contain any analyzers" [\#229](https://github.com/code-cracker/code-cracker/issues/229)
- Use enum for diagnostic ids [\#228](https://github.com/code-cracker/code-cracker/issues/228)
- Change verbatim string.format to use string interpolation [\#220](https://github.com/code-cracker/code-cracker/issues/220)
- Update CC to use VS 2015 CTP 5 [\#219](https://github.com/code-cracker/code-cracker/issues/219)
- Build with PSake and kill \(almost\) all scripts [\#215](https://github.com/code-cracker/code-cracker/issues/215)
- Remove trailling whitespace [\#201](https://github.com/code-cracker/code-cracker/issues/201)
- Validate IPAddress.Parse from System.Net [\#188](https://github.com/code-cracker/code-cracker/issues/188)
- Validade Uri from System.Uri [\#182](https://github.com/code-cracker/code-cracker/issues/182)
- Interfaces should start with an "I" [\#179](https://github.com/code-cracker/code-cracker/issues/179)
- If method returns a Task it should have the postfix "Async" [\#178](https://github.com/code-cracker/code-cracker/issues/178)
- Offer a fix for ordering members inside classes and structs following StyleCop patterns [\#172](https://github.com/code-cracker/code-cracker/issues/172)
- Abstract class ctors should not have public constructors [\#164](https://github.com/code-cracker/code-cracker/issues/164)
- Update string interpolation to use new version [\#146](https://github.com/code-cracker/code-cracker/issues/146)
- Allow formating on string interpolation substitutions of string.format [\#145](https://github.com/code-cracker/code-cracker/issues/145)
- Run Analysis on a large OSS project and make sure it does not throw [\#100](https://github.com/code-cracker/code-cracker/issues/100)
- Add code coverage metrics [\#99](https://github.com/code-cracker/code-cracker/issues/99)
- Update all analyzers to use the supported categories [\#97](https://github.com/code-cracker/code-cracker/issues/97)
- Detect read-only private fields and fix adding the "readonly" modifier [\#86](https://github.com/code-cracker/code-cracker/issues/86)
- Offer diagnostic to allow for ordering members inside classes and structs [\#76](https://github.com/code-cracker/code-cracker/issues/76)
- Excess parameters in methods [\#44](https://github.com/code-cracker/code-cracker/issues/44)
- Suggest use of stringbuilder when you have a while loop [\#34](https://github.com/code-cracker/code-cracker/issues/34)
- Private set by default for automatic properties [\#32](https://github.com/code-cracker/code-cracker/issues/32)
- Class that has IDisposable fields should implement IDisposable and dispose those fields [\#30](https://github.com/code-cracker/code-cracker/issues/30)
- IDisposable not assigned to a field is not being disposed [\#28](https://github.com/code-cracker/code-cracker/issues/28)
- Remove unused parameters [\#24](https://github.com/code-cracker/code-cracker/issues/24)
- Validate Json when used with Json.NET [\#2](https://github.com/code-cracker/code-cracker/issues/2)

**Fixed bugs:**

- BUG on string interpolation when indexes are inverted [\#246](https://github.com/code-cracker/code-cracker/issues/246)
- BUG on CC0049, simplify boolean comparison, not simmetric [\#238](https://github.com/code-cracker/code-cracker/issues/238)
- False positive on CC0049 \("You can remove this comparison"\) [\#236](https://github.com/code-cracker/code-cracker/issues/236)
- BUG on CC0032/3: DisposableFieldNotDisposedAnalyzer throwing when Dispose is abstract [\#227](https://github.com/code-cracker/code-cracker/issues/227)
- BUG on CC0012: RethrowExceptionAnalyzer throwing "Sequence contains no elements" [\#226](https://github.com/code-cracker/code-cracker/issues/226)
- BUG on CC0026: When there is a dynamic don't raise a diagnostic [\#225](https://github.com/code-cracker/code-cracker/issues/225)
- CC0029 should be reported for explicit IDisposable.Dispose implementation [\#222](https://github.com/code-cracker/code-cracker/issues/222)
- BUG on PrivateSetAnalyzer when there are no accessors [\#217](https://github.com/code-cracker/code-cracker/issues/217)
- BUG on UriAnalyzer when analyzing an expression [\#209](https://github.com/code-cracker/code-cracker/issues/209)
- BUG on ReadonlyFieldAnalyzer \(CC0052\) when analyzing partial classes [\#194](https://github.com/code-cracker/code-cracker/issues/194)
- Null Reference BUG on ForInArrayAnalyzer \(CC0006\) [\#193](https://github.com/code-cracker/code-cracker/issues/193)
- InvalidCastException in CC0016 and CC0031 [\#175](https://github.com/code-cracker/code-cracker/issues/175)
- Bug: CC0030 changes nullable type to const [\#167](https://github.com/code-cracker/code-cracker/issues/167)
- BUG: CC0009 \(from ObjectInitializerAnalyzer\) not being generated [\#165](https://github.com/code-cracker/code-cracker/issues/165)
- BUG on CC0047 \(private set\) should not report diagnostic when the property is being referenced outside the class [\#159](https://github.com/code-cracker/code-cracker/issues/159)

## [v1.0.0-alpha2](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha2) (2014-12-15)
[Full Changelog](https://github.com/code-cracker/code-cracker/compare/v1.0.0-alpha1...v1.0.0-alpha2)

**Implemented enhancements:**

- Change verbatim string from refactoring to Hidden diagnostic + code fix [\#161](https://github.com/code-cracker/code-cracker/issues/161)
- Modify CC0037 to make set accessor private when that have code [\#150](https://github.com/code-cracker/code-cracker/issues/150)
- Change from string.Format to string interpolation [\#133](https://github.com/code-cracker/code-cracker/issues/133)
- Simplify redundant Boolean comparisons [\#124](https://github.com/code-cracker/code-cracker/issues/124)
- Convert method body to expression bodied member when applicable [\#101](https://github.com/code-cracker/code-cracker/issues/101)
- Remove commented code [\#98](https://github.com/code-cracker/code-cracker/issues/98)
- Invert loop for 0..n and n..0 [\#87](https://github.com/code-cracker/code-cracker/issues/87)
- Make a variable const whenever it's possible [\#79](https://github.com/code-cracker/code-cracker/issues/79)
- Check null on event to avoid race condition when invoking it [\#61](https://github.com/code-cracker/code-cracker/issues/61)
- Struct vs. Keyword [\#59](https://github.com/code-cracker/code-cracker/issues/59)
- Detect direct event invocation [\#55](https://github.com/code-cracker/code-cracker/issues/55)
- Create a build server and build every push and pull request [\#53](https://github.com/code-cracker/code-cracker/issues/53)
- Suggest nameof when encounter a string with the same name of a parameter [\#40](https://github.com/code-cracker/code-cracker/issues/40)
- Suggest switch if you have 3 or more nested if / else statements [\#39](https://github.com/code-cracker/code-cracker/issues/39)
- Call extension method as an extension [\#27](https://github.com/code-cracker/code-cracker/issues/27)
- Empty Catch block not allowed [\#15](https://github.com/code-cracker/code-cracker/issues/15)
- Remove empty object initializers [\#14](https://github.com/code-cracker/code-cracker/issues/14)
- Use existence ?. operator when possible in expressions [\#13](https://github.com/code-cracker/code-cracker/issues/13)
- Remove unnecessary parenthesis from class initialization [\#11](https://github.com/code-cracker/code-cracker/issues/11)
- Use class initializer where it makes sense [\#10](https://github.com/code-cracker/code-cracker/issues/10)
- Replace if check followed by return true or false [\#8](https://github.com/code-cracker/code-cracker/issues/8)
- On Linq clauses move predicate from Where to First, Single, etc when applicable [\#6](https://github.com/code-cracker/code-cracker/issues/6)
- Always use var [\#4](https://github.com/code-cracker/code-cracker/issues/4)

**Fixed bugs:**

- BUG when using "use string interpolation" code fix with string that contains line breaks [\#162](https://github.com/code-cracker/code-cracker/issues/162)
- Bug when running codefix for CC0031 \(UseInvokeMethodToFireEvent\) on statement without block [\#160](https://github.com/code-cracker/code-cracker/issues/160)
- Bug: Private set on props being suggested when there are inheritted members referencing [\#153](https://github.com/code-cracker/code-cracker/issues/153)
- Bug when running codefix for CC0031 \(UseInvokeMethodToFireEvent\) on parameters [\#152](https://github.com/code-cracker/code-cracker/issues/152)
- BUG when make const sees an string interpolation \(CC0030\) [\#140](https://github.com/code-cracker/code-cracker/issues/140)
- NullReferenceException in ArgumentExceptionAnalyzer [\#111](https://github.com/code-cracker/code-cracker/issues/111)
- When changing ifs to switch comments are lost [\#74](https://github.com/code-cracker/code-cracker/issues/74)
- TernaryOperatorAnalyzer throwing NullReferenceException [\#70](https://github.com/code-cracker/code-cracker/issues/70)
- Regex Analyzer/CodeFix tests ignores local culture [\#69](https://github.com/code-cracker/code-cracker/issues/69)

## [v1.0.0-alpha1](https://github.com/code-cracker/code-cracker/tree/v1.0.0-alpha1) (2014-11-12)
**Implemented enhancements:**

- Change if to ternary operator [\#7](https://github.com/code-cracker/code-cracker/issues/7)
- Transform for into a foreach [\#3](https://github.com/code-cracker/code-cracker/issues/3)



\* *This Change Log was automatically generated by [github_changelog_generator](https://github.com/skywinder/Github-Changelog-Generator)*