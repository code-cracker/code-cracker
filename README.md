# Code Cracker

An analyzer library for C# and VB that uses [Roslyn](https://github.com/dotnet/roslyn) to produce refactorings, code analysis, and other niceties. 

Check the official project site on [code-cracker.github.io](http://code-cracker.github.io). There you will find information on how to contribute,
our task board, definition of done, definition of ready, etc.

[![Build status](https://ci.appveyor.com/api/projects/status/h21sli3jkumuswyi?svg=true)](https://ci.appveyor.com/project/code-cracker/code-cracker)
[![Nuget count](https://img.shields.io/nuget/v/codecracker.CSharp.svg)](https://www.nuget.org/packages/codecracker.CSharp/)
[![License](https://img.shields.io/github/license/code-cracker/code-cracker.svg)](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt)
[![Issues open](https://img.shields.io/github/issues-raw/code-cracker/code-cracker.svg)](https://huboard.com/code-cracker/code-cracker/)
[![Coverage Status](https://img.shields.io/coveralls/code-cracker/code-cracker/master.svg)](https://coveralls.io/r/code-cracker/code-cracker?branch=master)
[![Source Browser](https://img.shields.io/badge/Browse-Source-green.svg)](http://ccref.azurewebsites.net)

You can find this document in the following languages

[![English](https://img.shields.io/badge/language-english-blue.svg)](https://github.com/code-cracker/code-cracker/blob/master/README.md)
[![Brazilian Portuguese](https://img.shields.io/badge/language-brazilan%20portuguese-brightgreen.svg)](https://github.com/code-cracker/code-cracker/blob/master/README.pt.md)


This is a community project, free and open source. Everyone is invited to contribute, fork, share and use the code. No money shall be charged by this 
software, nor it will be. Ever.

## Features

The list of features is documented here: http://code-cracker.github.io/diagnostics.html

#### Design
Code | Analyzer | Severity | Description 
-- | -- | -- | --
[CC0003](http://code-cracker.github.io/diagnostics/CC0003.html) | CatchEmptyAnalyzer | Warning | Catch statements with no Exception as an argument is not recommended. Consider adding an Exception class to the catch statement.
[CC0004](http://code-cracker.github.io/diagnostics/CC0004.html) | EmptyCatchBlockAnalyzer | Warning | An empty catch block suppress all errors and shouldn’t be used. If the error is expected consider logging it or changing the control flow such that it is explicit.
[CC0016](http://code-cracker.github.io/diagnostics/CC0016.html) | CopyEventToVariableBeforeFireAnalyzer | Warning | Events should always be checked for null before being invoked. As in a multi-threading context, it is possible for an event to be unsubscribed between the moment where it is checked to be non-null and the moment it is raised, the event must be copied to a temporary variable before the check.
[CC0021](http://code-cracker.github.io/diagnostics/CC0021.html) | NameOfAnalyzer | Warning | In C#6 the nameof() operator should be used to specify the name of a program element instead of a string literal as it produces code that is easier to refactor.
[CC0024](http://code-cracker.github.io/diagnostics/CC0024.html) | StaticConstructorExceptionAnalyzer | Warning | Static constructor are called before the first time a class is used but the caller doesn’t control when exactly. Exceptions thrown in this context force callers to use ‘try’ block around any usage of the class and should be avoided.
[CC0031](http://code-cracker.github.io/diagnostics/CC0031.html) | UseInvokeMethodToFireEventAnalyzer | Warning | In C#6 a delegate can be invoked using the null-propagating operator (?.) and its invoke method to avoid throwing a NullReference exception when there is no method attached to the delegate.

## Installing

You may use CodeCracker in two ways: as an analyzer library that you install with NuGet into your project, or as a Visual Studio extension.
The way you want to use it depends on the scenario you are working on. You most likely want the Nuget package.

If you want the analyzers to work during your build, and generate warnings and errors during the build, also on build servers, then you want
to use the Nuget package. The package is available on NuGet ([C#](https://www.nuget.org/packages/codecracker.CSharp),
[VB](https://www.nuget.org/packages/codecracker.VisualBasic)).
If you want to be able to configure which analyzers are being used in your project, and which ones you will ignore, and commit those
changes to source control and share with your team, then you also want the Nuget package.

To install from NuGet, for the C# version:

```powershell
Install-Package CodeCracker.CSharp
```

Or for the Visual Basic version:

```powershell
Install-Package CodeCracker.VisualBasic
```

Or use the Package Manager in Visual Studio.

There is also a version for both named `CodeCracker` only, but it makes no sense to get it, you should search for the C# or VB version.

If you want the alpha builds that build on each push to the repo, add [this](https://www.myget.org/F/codecrackerbuild/) to your NuGet feed.
We only push complete releases to Nuget.org, and commit builds go to Myget.org.

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

TL;DR:
If you want to use CodeCracker in all your projects, install the Visual Studio extension ([C#](https://visualstudiogallery.msdn.microsoft.com/ab588981-91a5-478c-8e65-74d0ff450862), [VB](https://visualstudiogallery.msdn.microsoft.com/1a5f9551-e831-4812-abd0-ac48603fc2c1)). If you want to use CodeCracker for just one project, install the Nuget package as described above.

## SonarQube Plugin

CodeCracker has a SonarQube Plugin that can be downloaded at [Plugins HomePage](http://docs.sonarqube.org/display/PLUG/Other+Plugins).

## Contributing [![Open Source Helpers](https://www.codetriage.com/code-cracker/code-cracker/badges/users.svg)](https://www.codetriage.com/code-cracker/code-cracker)

The main supported IDE for development is Visual Studio 2017.
We do not support VS 2015 anymore.

Questions, comments, bug reports, and pull requests are all welcome.
Bug reports that include steps to reproduce (including code) are
preferred. Even better, make them in the form of pull requests.
Before you start to work on an existing issue, check if it is not assigned
to anyone yet, and if it is, talk to that person.
Also, check the project [board](https://huboard.com/code-cracker/code-cracker/)
and verify it is not being worked on (it will be tagged with the `Working` tag).
If it is not being worked on, check if the item is `Ready` before you start.
If the issue has the `Working` tag (working swimlane on Huboard) and has no Assignee
then it is not being worked on by somebody on the core team. Check the issue's
description to find out who it is (if it is not there it has to be on the comments).
We are adding people that want to contribute to the `Contributors` team so we can
always assign them to the issues, so during your first contribution you probably will
be assigned to this team.

The easiest way to start is looking into the issues that are
[up for grabs](https://github.com/code-cracker/code-cracker/labels/up-for-grabs). You
may ask to work on any of them, read below to see how. You can also triage issues which may include reproducing bug reports or asking for vital information such as version numbers or reproduction instructions. If you would like to start triaging issues, one easy way to get started is to [subscribe to code-cracker on CodeTriage](https://www.codetriage.com/code-cracker/code-cracker).

If you are just starting with Roslyn, want to contribute, and feel you are not yet ready
to start working on full analyzers or code fixes, you can start helping with areas that are
less demanding. We have identified a few:

* Fixing bugs

  Still demands knowledge of Roslyn internals but it is easier than coming up with a full analyzer or code fix. Look for the [bugs that are up for grabs](https://github.com/code-cracker/code-cracker/issues?utf8=%E2%9C%93&q=is%3Aopen+label%3Abug+label%3Aup-for-grabs).

* Documentation

  We are documenting all analyzers on the [CodeCracker user site](http://code-cracker.github.io/diagnostics.html).
  There are lots of analyzers and code fixes to document.

* Localization/Translation

  We are starting to translate the analyzers and code fixes messages to other languages. If you want CodeCracker on your language feel free to create an issue and start working on it. If you want to help with an ongoing translation,
  comment on the existing issue and say you are ready to help. We also need to update existing analyzers, which were not created ready for localization.

## Issues and task board

* The task board is at [Huboard](https://huboard.com/code-cracker/code-cracker/).
* You can also check the [Github backlog](https://github.com/code-cracker/code-cracker/issues) directly.

### Definition of Ready (DoR)

An item should only have its work started after the backlog item is ready. We have
defined ready as:

1. Have most of the scenarios/test cases defined on the issue on Github
2. If it has an analyzer then

        1. The warning level of the analyzer must be in the issue's description (`Hidden`, `Information`, `Warning`, or `Error`)
        2. The diagnostics it provides should already have numeric ids defined formatted as `CC0000`.
3. If it has a code fix then the category should be in the issue's description. The supported categories are listed on the `SupportedCategories.cs` file.
4. Have some of the maintainers verify it (cannot be the same one who wrote the issue and/or test cases)

The first one is important so we have clearly defined what we will build. The last one
is important so we don't go on building something that will not be usable, will hurt users, or will
be a waste of effort.

View examples at issues [#7](https://github.com/code-cracker/code-cracker/issues/7)
and [#10](https://github.com/code-cracker/code-cracker/issues/10).

### Severity Levels

These are the 4 severity levels supported on Roslyn and how they are understood on the Code Cracker project:

1. **Hidden**: Only used for refactorings. See #66 (and its comments) to understand why.
2. **Info**: An alternative way (ex: replacing for with foreach). Clearly, a matter of opinion and/or current way could be correct, or maybe the new code could be correct. We cannot determine.
3. **Warning**: Code that could/should be improved. It is a code smell and most likely is wrong, but there are situations where the pattern is acceptable or desired.
4. **Error**: Clearly a mistake (ex: throwing ArgumentException with a non-existent parameter). There is no situation where this code could be correct. There are no differences of opinion.

You can read [directly on Microsoft's source code](http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis/Diagnostic/DiagnosticSeverity.cs,e70281df673d47f6,references)
how they interpret these levels.

### Definition of Done

The DoD is still evolving. At the present time the checklist is as follows:

1. Builds
2. Has tests for analyzers, code fixes (including fix all providers) and refactoring
3. All tests pass
4. Analyzers follow the guidelines for names

        1. Always named `<featurename>Analyzer`
        2. Always add the diagnostic id to the `DiagnosticIds.cs` file.
5. Code fixes should follow the guidelines for names

        1. Always named `<featurename>CodeFixProvider`
        2. Always use the same diagnostic id added to the `DiagnosticIds.cs` file, unless you are writing a code fix for a diagnostic id raised by the C# compiler itself (starting with `CS`).
6. Fix all scenarios (fix all in a document, fix all in a project and fix all in solution) work. You might need to write a `FixAllProvider`. Check the `DisposableVariableNotDisposedFixAllProvider` as an example.
7. Follow the coding standards present on the project code files.
8. Works in Visual Studio
9. Uses localizable strings

### Start working

Once it is Ready and agreed on by anyone from the core team, just state in
a comment that you intend to start working on that item and mention any/all
the maintainers (use @code-cracker/owners) so they can tag it correctly and move it on the board.

If you are not familiar with the way Github works you might want to check the [Github guides](https://guides.github.com/), especially
the [Github flow](https://guides.github.com/introduction/flow/) one. The
[GitHub for the Roslyn Team video](http://channel9.msdn.com/Blogs/dotnet/github-for-the-roslyn-team) might help you as well, and
it also explains some Git concepts.

To start working fork the project on Github to your own account and clone it **from there**. Don't clone
from the main CodeCracker repository. Before you start coding create a new branch and name it in a way that makes
sense for the issue that you will be working on. Don't work on the `master` branch because that may make
things harder if you have to update your pull request or your repository later, assume your `master` branch
always equals the main repo `master` branch, and code on a different branch.

When you commit, mention the issue number use the pound sign (#). Avoid making a lot of small commits
unless they are meaningful. For most analyzers and code fixes a single commit should be enough. If you
prefer to work with a lot commits at the end squash them.

Make your first commit lines mean something, specially the first one.
[Here](https://robots.thoughtbot.com/5-useful-tips-for-a-better-commit-message) and
[here](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html) are some tips on a good
commit first line/message.

**Do not**, under any circumstance, reformat the code to fit your standards. Follow the project standards,
and if you do not agree with them discuss them openly with the Code Cracker community. Also, avoid end of line
white spaces at all costs. Keep your code clean.

Always write unit tests for your analyzers, code fixes, fix all providers and analyzers.

### Pull request

When you are done, pull the changes from the `master` branch on the main CodeCracker repo and integrate them.

You have to do that in the command line:
````bash
# add the main repo with the `codecracker` name
git remote add code-cracker https://github.com/code-cracker/code-cracker.git
# checkout the master branch
git checkout master
# download the latest changes from the master repo
git pull code-cracker master
# go back to your working branch
git checkout <youbranchname>
# integrate your changes
git merge master
# solve integration conflicts
````

You can solve the conflicts in your favorite text editor or Visual Studio.
Visual Studio actually presents the conflict in a very nice way to solve them.
Also, on the `go back to your working branch` step you can go back to using Visual Studio to control git if you
prefer that.

If you know git well, you can rebase your changes instead of merging them. If not, it is ok to merge them.
When your changes are up to date with the
`master` branch then you should push them to your Github repo and then you will be able to issue
a [pull request](https://help.github.com/articles/using-pull-requests/) and
mention the issue you were working on. Make your PR message clear. If when you are creating the pull request on
Github it mentions that the PR cannot be merged because there are conflicts it means you forgot to integrate
the `master` branch. Correct that push the changes to your personal repo. This will automatically update the PR.
The project maintainers should not have to resolve merge conflicts, you should.

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
* One code issue per person at a time (blocked issues don't count)
* Your pull requested will be commented by the Coveralls bot. Make sure code coverage has not gone down significantly. Ideally, it should go up.

If you work on something that you have not yet discussed with the maintainers
there is a chance the code might be denied because they might find the analyzer/fix is not necessary, duplicated, or some other reason.
They are easily reachable through Twitter or on Github. Before you code discuss it with it them.

Small code changes or updates outside code files will eventually be made by the core team directly on `master`, without a PR.


## Maintainers/Core team

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), aka Giggio, [Lambda3](http://www.lambda3.com.br), [@giovannibassi](https://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [Promob](http://promob.com/), [@elemarjr](https://twitter.com/elemarjr)
* [Carlos dos Santos](http://carloscds.net/), [CDS Informática](http://www.cds-software.com.br/), [@cdssoftware](https://twitter.com/cdssoftware)

Contributors can be found at the [contributors](https://github.com/code-cracker/code-cracker/graphs/contributors) page on Github.

### What are the maintainers' responsibilities?

The maintainers have to:

* Commit regularly.
* Work on project maintenance tasks regularly, like (but not limited to)
  * attend meetings,
  * review pull requests,
  * create and discuss on project issues.

To become part of the core team one has to be invited. Invitations happen only if all the core team agrees.

If a member of the core team is not active for at least two months, they will probably be removed from the core team.

## Contact

Please see our [contact page](http://code-cracker.github.io/contact.html).

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
