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

Questions, comments, bug reports, and pull requests are all welcome.
Bug reports that include steps-to-reproduce (including code) are the
best. Even better, make them in the form of pull requests.
Before you start to work on an existing issue, check if it is not assigned
to anyone yet, and if it is, talk to that person.
Also check the project [board](http://huboard.com/giggio/CodeCracker/board)
and verify it is not being worked on (it will also be tagged with the `working` tag).
If it is not being worked on, before you start check if the item is `ready`.
We don't yet have a Definition Of Ready (DOR) yet, but right now being ready means
having at least the basic test cases written on the item. View examples at
issues [#7](https://github.com/giggio/CodeCracker/issues/7)
and [#10](https://github.com/giggio/CodeCracker/issues/10).
Once it is Ready and agreed on by any one of the maintainers, just state in
a comment that you intend to start working on that item and mention any/all
the mainteners so they can tag it correctly and move it on the board.
When you are done, issue a [pull request](https://help.github.com/articles/using-pull-requests/) and mention the issue you were working on.

### Rules for contribution

* Every pull request must have unit tests. PRs without tests will be denied without checking any other code.
* Must build and all tests must pass
* Must mention an existing issue on Github
* Don't reformat any code but the one you produced
* Follow the coding standards already in place within the project

If you work on something that you have not yet discussed with the maintainers
there is a big chance the code might be denied.
They are easily reachable through Twitter or on Github. Before you code discuss it with it them.

## Maintainers

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), [Lambda3](http://www.lambda3.com.br), [@giovannibassi](http://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [@elemarjr](http://twitter.com/elemarjr)
* [Carlos dos Santos](http://carloscds.net/), [CDS Informática](http://www.cds-software.com.br/), [@cdssoftware](http://twitter.com/cdssoftware)
* [Vinicius Hana](https://blog.lambda3.com.br/L3/vinicius-hana/), [Lambda3](http://www.lambda3.com.br), [@viniciushana](http://twitter.com/viniciushana)

Contributors can be found at the [contributors](https://github.com/giggio/CodeCracker/graphs/contributors) page on Github.

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/giggio/CodeCracker/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project, and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
