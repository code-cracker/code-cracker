#New analyzer:

Input your analyzer description.

Before:

````csharp
//your code that triggers the diagnostic
````

After:

````csharp
//your code after the fix has been applied
````

You can add more information here, e.g. conditions under which a diagnostic should not trigger, etc.

Diagnostic Id: `CC0000` (take a number and update the [wiki](https://github.com/code-cracker/code-cracker/wiki/DiagnosticIds))
Category: `<some category>` (see [supported categories](https://github.com/code-cracker/code-cracker/blob/master/src/Common/CodeCracker.Common/SupportedCategories.cs) and their [descriptions](https://github.com/code-cracker/code-cracker/issues/97))
Severity: `Hidden | Info | Warning | Error` (see the [descriptions](https://github.com/code-cracker/code-cracker/#severity-levels))

#Bug

Input your bug description. Make sure you describe the steps to reproduce,
that you are working with the latest version, and the issue has not been reported yet.

Example: (don't use your project code, use a sample that anyone could use to verify the bug,
so, for example,
don't use classes that are not part of the BCL or declared on your sample.)

````csharp
//the code that reproduces the bug
````

Current output after fix applied (if it is a code fix bug):

````csharp
//code fixed incorrectly
````

Expected output after fix applied (if it is a code fix bug):

````csharp
//code fixed incorrectly
````