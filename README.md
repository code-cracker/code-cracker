# Code Cracker

An analyzer library for C# that uses [Roslyn](http://msdn.microsoft.com/en-us/vstudio/roslyn.aspx) to produce refactorings, code analysis, and other niceties.

Check the official project site on [code-cracker.github.io](http://code-cracker.github.io). There you will find information on how to contribute,
our task board, definition of done, definition of ready, etc.

[![Build status](https://ci.appveyor.com/api/projects/status/h21sli3jkumuswyi?svg=true)](https://ci.appveyor.com/project/code-cracker/code-cracker)
[![Nuget count](http://img.shields.io/nuget/v/codecracker.svg)](https://www.nuget.org/packages/codecracker/)
[![Nuget downloads](http://img.shields.io/nuget/dt/codecracker.svg)](https://www.nuget.org/packages/codecracker/)
[![Issues open](http://img.shields.io/github/issues-raw/code-cracker/code-cracker.svg)](https://huboard.com/code-cracker/code-cracker)
[![Coverage Status](https://img.shields.io/coveralls/code-cracker/code-cracker/master.svg)](https://coveralls.io/r/code-cracker/code-cracker?branch=master)
[![Source Browser](https://img.shields.io/badge/Browse-Source-green.svg)](http://sourcebrowser.io/Browse/code-cracker/code-cracker)

This is a community project, free and open source. Everyone is invited to contribute, fork, share and use the code. No money shall be charged by this
software, nor it will be. Ever.

## Installing

You may use CodeCracker in two ways: as an analyzer library that you install with Nuget into your project or as a Visual Studio extension.
The way you want to use it depends on the scenario you are working on. You most likely want the Nuget package.

If you want the analyzers to work during your build, and generate warnings and errors during the build, also on build servers, then you want
to use the Nuget package. The package is available on nuget ([C#](https://www.nuget.org/packages/codecracker.CSharp),
[VB](https://www.nuget.org/packages/codecracker.VisualBasic)).
If you want to be able to configure which analyzers are being used in your project, and which ones you will ignore, and commit those
changes to source control and share with your team, then you also want the Nuget package.

To install from Nuget, for the C# version:

```powershell
Install-Package CodeCracker.CSharp
```

Or for the Visual Basic version:

```powershell
Install-Package CodeCracker.VisualBasic
```

Or use the Package Manager in Visual Studio.

There is also a version for both named `CodeCracker` only, but it makes not sense to get it, you should search for the C# or VB version.

If you want the alpha builds that build on each push to the repo, add https://www.myget.org/F/codecrackerbuild/ to your nuget feed.
We are now only pushing complete alpha releases to Nuget.org, and commit builds go to Myget.org.

If you want global analyzers that will work on every project you open in Visual Studio, then you want the Extension.
Grab the extension at the Visual Studio Extensions Gallery ([C#](https://visualstudiogallery.msdn.microsoft.com/ab588981-91a5-478c-8e65-74d0ff450862),
[VB](https://visualstudiogallery.msdn.microsoft.com/1a5f9551-e831-4812-abd0-ac48603fc2c1)).

To build from source:

```shell
git clone https://github.com/code-cracker/code-cracker.git
cd CodeCracker
msbuild
```

Then add a reference to CodeCracker.dll from within the Analyzers node inside References, in Visual Studio.

## Maintainers

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), aka Giggio, [Lambda3](http://www.lambda3.com.br), [@giovannibassi](http://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [Promob](http://promob.com/), [@elemarjr](http://twitter.com/elemarjr)
* [Carlos dos Santos](http://carloscds.net/), [CDS Informática](http://www.cds-software.com.br/), [@cdssoftware](http://twitter.com/cdssoftware)
* [Vinicius Hana](https://blog.lambda3.com.br/L3/vinicius-hana/), [Lambda3](http://www.lambda3.com.br), [@viniciushana](http://twitter.com/viniciushana)

Contributors can be found at the [contributors](https://github.com/code-cracker/code-cracker/graphs/contributors) page on Github.

## Contact

Please see our [contact page](http://code-cracker.github.io/contact.html).

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project, and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
