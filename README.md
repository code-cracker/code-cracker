# Code Cracker

A analyzer library for C# that uses [Roslyn](http://msdn.microsoft.com/en-us/vstudio/roslyn.aspx) to produce refactorings, code analysis, and other niceties.

This is a community project, free and open source. Everyone is invited to contribute, fork, share and use the code. No money shall be charged by this
software, nor it will be. Ever.

## Installing

You may use CodeCracker in two ways: as an analyzer library that you install with Nuget into your project or as a Visual Studio extension.
The way you want to use it depends on the scenario you are working on. You most likely want the Nuget package.

If you want the analyzers to work during your build, and generate warnings and errors during the build, also on build servers, than you want
to use the Nuget package.
If you want to be able to configure which analyzers are being used in your project, and which ones you will ignore, and commit those
changes to source control and share with your team, then you also want the Nuget package.

To install from Nuget (TODO: add package to Nuget):

```powershell
Install-Package CodeCracker
```

Or use the Package Manager in Visual Studio.

If you want global analyzers that will work on every project you open in Visual Studio, then you want the Extension.
Grab the extension at the Visual Studio Extensions site: (TODO: add site)

To build from source:

```shell
git clone https://github.com/giggio/CodeCracker.git
cd CodeCracker
msbuild
```

Then add a reference to CodeCracker.dll from within the Analyzers node inside References, in Visual Studio.

## Issues and task board

* The task board is at [Huboard](http://huboard.com/giggio/CodeCracker/board).
* You can also check the [Github backlog](https://github.com/giggio/CodeCracker/issues) directly.

## Contributing

Questions, comments, bug reports, and pull requests are all welcome.  Submit them at
[the project on GitHub](https://github.com/giggio/CodeCracker/).

Bug reports that include steps-to-reproduce (including code) are the
best. Even better, make them in the form of pull requests.

## Maintainers

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), [Lambda3](http://www.lambda3.com.br), [@giovannibassi](http://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [@elemarjr](http://twitter.com/elemarjr)

Contributors can be found at the [contributors](https://github.com/giggio/CodeCracker/graphs/contributors) page on Github.

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/giggio/CodeCracker/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project, and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
