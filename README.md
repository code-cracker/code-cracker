# Code Cracker

An analyzer library for C# that uses [Roslyn](http://msdn.microsoft.com/en-us/vstudio/roslyn.aspx) to produce refactorings, code analysis, and other niceties.

This is a community project, free and open source. Everyone is invited to contribute, fork, share and use the code. No money shall be charged by this
software, nor it will be. Ever.

## Installing

You may use CodeCracker in two ways: as an analyzer library that you install with Nuget into your project or as a Visual Studio extension.
The way you want to use it depends on the scenario you are working on. You most likely want the Nuget package.

If you want the analyzers to work during your build, and generate warnings and errors during the build, also on build servers, than you want
to use the Nuget package. The package is available on nuget at [nuget.org/packages/codecracker](https://www.nuget.org/packages/codecracker).
If you want to be able to configure which analyzers are being used in your project, and which ones you will ignore, and commit those
changes to source control and share with your team, then you also want the Nuget package.

To install from Nuget:

```powershell
Install-Package CodeCracker
```

Or use the Package Manager in Visual Studio.

If you want global analyzers that will work on every project you open in Visual Studio, then you want the Extension.
Grab the extension at the Visual Studio Extensions site: (TODO: add site)

To build from source:

```shell
git clone https://github.com/code-cracker/code-cracker.git
cd CodeCracker
msbuild
```

Then add a reference to CodeCracker.dll from within the Analyzers node inside References, in Visual Studio.

## Issues and task board

* The task board is at [Huboard](http://huboard.com/code-cracker/code-cracker/board).
* You can also check the [Github backlog](https://github.com/code-cracker/code-cracker/issues) directly.

## Contributing

Questions, comments, bug reports, and pull requests are all welcome.
Bug reports that include steps-to-reproduce (including code) are the
best. Even better, make them in the form of pull requests.
Before you start to work on an existing issue, check if it is not assigned
to anyone yet, and if it is, talk to that person.
Also check the project [board](http://huboard.com/code-cracker/code-cracker/board)
and verify it is not being worked on (it will also be tagged with the `working` tag).
If it is not being worked on, before you start check if the item is `ready`.

### Definition of Ready (DoR)

An item should only have its work started after the backlog item is ready. We have
defined ready as:

1. Have most of the scenarios/test cases defined on the issue on Github
2. Have some of the maintainers verify it (cannot be the same one who wrote the issue and/or test cases)

The first one is important so we have clearly defined what we will build. The second one
is important so we don't go on building something that will not be usable, will hurt users, or will
be a waste of effort.

View examples at issues [#7](https://github.com/code-cracker/code-cracker/issues/7)
and [#10](https://github.com/code-cracker/code-cracker/issues/10).

### Start working

Once it is Ready and agreed on by any one of the maintainers, just state in
a comment that you intend to start working on that item and mention any/all
the maintainers (use @code-cracker/owners) so they can tag it correctly and move it on the board.

To start working fork the project on Github to your own account and clone it **from there**. Don't clone
from the main CodeCracker repository. Before you start coding create a new branch and name it in a way that makes
sense for the issue that you will be working on. Don't work on the `master` branch because that may make
things harder if you have to update your pull request or your repository later, assume your `master` branch
is always equals the main repo `master` branch, and code on a different branch.

When you commit, mention the issue number use the pound sign (#). Avoid making a lot of small commits
unless they are meaningful. For most analyzers and code fixes a single commit should be enough. If you
prefer to work with a lot commits at the end squash them.

Make your first commit lines mean something, specially the first one.
[Here](http://robots.thoughtbot.com/5-useful-tips-for-a-better-commit-message) and
[here](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html) are some tips on a good
commit first line/message.

**Do not**, under any circumstance, reformat the code to fit your standards. Follow the project standards,
and if you do not agree with them discuss them openly with the Code Cracker community. Also, avoid end of line
white spaces at all costs. Keep your code clean.

Always write unit tests for your analyzers, code fixes and analyzers.

### Pull request

When you are done, pull the changes from the `master` branch on the main CodeCracker repo and integrate them. If
you know git well, rebase your changes. If not, it is ok to merge them. When your changes are up to date with the
`master` branch then issue a [pull request](https://help.github.com/articles/using-pull-requests/) and
mention the issue you were working on. Make your PR message clear. If when you are creating the pull request on
Github it mentions that the PR cannot be merged because there are conflicts it means you forgot to integrate
the `master` branch. Correct that and issue the PR again. The project maintainers should not have to resolve
merge conflicts.

After your pull request is accepted you may delete your local branch if you want. Update your `master` branch
so you can continue to contribute in the future. And thank you! :)

If your pull request is denied try to understand why. It is not uncommon that PRs are denied but after some
discussing and fixing they are accepted. Work with the community to get it to be the best possible code. And thank you!

### Rules for contribution

* Every pull request must have unit tests. PRs without tests will be denied without checking anything else.
* Must build and all tests must pass
* Must mention an existing issue on Github
* Don't reformat any code but the one you produced
* Follow the coding standards already in place within the project

If you work on something that you have not yet discussed with the maintainers
there is a chance the code might be denied.
They are easily reachable through Twitter or on Github. Before you code discuss it with it them.

## Maintainers

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), aka Giggio, [Lambda3](http://www.lambda3.com.br), [@giovannibassi](http://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [@elemarjr](http://twitter.com/elemarjr)
* [Carlos dos Santos](http://carloscds.net/), [CDS Informática](http://www.cds-software.com.br/), [@cdssoftware](http://twitter.com/cdssoftware)
* [Vinicius Hana](https://blog.lambda3.com.br/L3/vinicius-hana/), [Lambda3](http://www.lambda3.com.br), [@viniciushana](http://twitter.com/viniciushana)

Contributors can be found at the [contributors](https://github.com/code-cracker/code-cracker/graphs/contributors) page on Github.

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project, and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
