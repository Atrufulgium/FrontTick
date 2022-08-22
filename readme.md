<!-- omit in toc -->
FrontTick
======
The aim of this project is to simplify creating behavioural Minecraft data packs. As you're probably aware if you found this repo, writing `.mcfunction` files by hand can be a massive pain; the arithmetic is verbose, simple if-statements usually result in several files, loops require recursion, etc.

These things would not be a problem if you had some more sensible language, (say, c#) and compiled down into data packs. That is exactly the purpose of this repo.

<!-- omit in toc -->
Table of Contents
======
- [Supported Features](#supported-features)
  - [Data types](#data-types)
  - [Data transformation](#data-transformation)
  - [Control flow](#control-flow)
  - [Minecraft-specific](#minecraft-specific)
  - [Higher-level Minecraft framework](#higher-level-minecraft-framework)
  - [Other](#other)
- [Project Setup](#project-setup)
- [Using this project](#using-this-project)
- [Why the name?](#why-the-name)

Supported Features
======
What follows is a list of features that have been implemented *and* unimplemented. Think of it both like a sort of to-do list, in no particular order, and like a reference of what you can do.

For a short tl;dr: no, &lt;feature you care about&gt; isn't implemented yet, and you can't reference *anything* but `MCMirror`. Which should be included in code-form.

Data types
------
- [x] Integers.
- [ ] Built-in value types other than `int`.
- [ ] Static fields.
- [ ] Static properties.
- [ ] General structs.
- [ ] Static arrays.
- [ ] Strings.
- [ ] Objects.
- [ ] Recursive data types.
- [ ] Genericism.
- [ ] Inheritance.

Data transformation
------
- [x] Assignments of the form `=` or `∘=` where `∘` is one of `+`, `-`, `*`, `/`, `%`.
- [ ] Arbitrary arithmetic.
- [x] Static function calls.
- [ ] General function calls.
- [x] Return at the end of a method.
- [ ] General return.
- [ ] `ref`, `in`, `out` modifiers.
- [ ] Recursive function calls.

Control flow
------
- [x] Branching by comparing to constants.
- [ ] General branching.
- [x] Goto.
- [ ] Switches.
- [ ] Switches' `goto case`.
- [ ] `for`, `while`, `do while` with `break` and `continue`.
- [ ] `foreach`.
- [ ] Throw simple exceptions.

Minecraft-specific
------
- [x] `[MCFunction]` entrypoints to code.
- [ ] Tick (any interval) and load code.
- [x] Literal `mcfunction` code.
- [ ] Selectors + `foreach` over selectors.
- [ ] World-specific stuff like `/weather` and `/gamerule`.
- [ ] Read/write blocks.
- [ ] Read/write (block) entities' nbt. (Wiki autogen letsgo.)

Higher-level Minecraft framework
------
- [ ] Custom items.
- [ ] Custom mobs.
- [ ] In a *very* far future: Blockbench support for the above custom mobs.

Other
------
- [x] A very simple in-game testing framework.
- [ ] MSIL support instead of just c# code.
- [ ] Proper documentation.

Project Setup
======
To be honest I still don't get how VS works around this kind of stuff. I'm using Roslyn via the packages listed at [the Compiler project's .csproj file](./Compiler/Compiler/Compiler.csproj). In any case, there are four projects in the repo:

- [**`Compiler`**](./Compiler/Compiler):
  As the name suggests, this is where the developement of the compiler happens.
  <br/>
  (References Roslyn and `MCMirror`.)
- [**`CompilerTests`**](./Compiler/CompilerTests):
  Testing the compiler on the c# side of things. These tests are mainly to check whether the transformations work as prescribed, whether the resulting data pack is correct is irrelevant.
  <br/>
  (References `Compiler`.)
- [**`IngameTests`**](./Compiler/IngameTests):
  Testing the compiler on the Minecraft side of things. These tests do not care about the process of the compiler, but instead test whether the resulting data pack is correct.
  <br/>
  (References `MCMirror`.)
- [**`MCMirror`**](./Compiler/MCMirror):
  A project to map Minecraft (implementable) features to c# usage. This is what end-users will use when creating data packs.
- There will at some point be a fifth project as sample.

Using this project
======
In order to use this project, in addition to patience, you need two things:

- The compiler itself. You'll probably need to build it from source but maybe you live in the far future where the project's far enough to have releases, *wow*!
  <br/>
  In order to use the compiler, there's four options you can read about in [Program.cs](./Compiler/Compiler/Program.cs). I know, the use-friendliness is off the charts (in the wrong direction). In general, you specify a directory to compile, a directory where `MCMirror` lives, a Minecraft worldname, and optionally some namespace.
- The `MCMirror` project. You can reference it all you like in dll form or whatever, but you also need to have the code-form somewhere on your machine to point the compiler to. As such it's convenient to just keep it next to the project  you're interested in.

At some point in the far future this will be made more streamlined than this jank.

Why the name?
======
Because `backtick` exists as a unicode character already.