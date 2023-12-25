FrontTick gotchas
=================
- FrontTick currently targets Minecraft 1.19.4.
- My F5 button builds the thing and runs it with the following command line args:  
  `-c ./IngameTests -m ./MCMirror -w FrontTick`  
  Especially the `FrontTick` part may need to be updated as it needs to be a valid Minecraft world save directory name.
- FrontTick doesn't use Roslyn too intuitively, it instead builds an abstraction layer above with `XXXWalker` and `XXXRewriter` classes with dependencies that get executed in order. Each phase has access to a `CurrentSemantics` semantic model.  
  At this point the phase order is a mess and I only know it due to the compiler output.
- In a lot of places you care about types. Unfortunately, importing them from MCMirror directly is not an option. For that reason, the `MCMirrorTypes.cs` file holds *manual* definitions of stuff *manually* annotated `[CompilerUsesName]` which you can then use in `CurrentSemantics.TypesMatch(node, MCMirrorTypes.Whatever)` or something.
- You (or rather, I) can't build the `MCMirror` directory manually in VS, see [the batch file](./Compiler/mcmirror_to_dll.bat).
- Throughout the entire compilation process, less and less c# features become available to you. Keep note.
- When your tests fail when stuff *looks* the same, ensure you don't have any `\n    \n` vs `\n\n` comparisons that didn't get removed when shift-tabbing.
- Recursion is **not supported**, but it **doesn't throw errors yet**.
- `const` is **not a thing yet**, but it **doesn't throw errors**.
- If your in-game tests return `-2122222222`, it means that nothing was returned at all. This may be a sign of malformed `mcfunction` code, check your launcher logs or manually review the generated pack.
- End-users may think some classes are available while in reality they are part of `_Unimplemented.cs`.
- Do a little prayer if you need to touch `GotoFlagifyRewriter.cs`.

Roslyn gotchas
==============
- Don't try to guess what syntax trees should look like, just use [Roslyn Quoter](roslynquoter.azurewebsites.net).
- Creating a compilation requires you to reference *all* dlls you need, including the basic stuff that defines `System` etc, it doesn't do it for you.  
  (For me this is an upside because I don't need them.)
- *All* Roslyn syntax factory methods are the name of the thing without the word `Syntax`. This is usually obvious, but this convention also holds for things like `SeparatedSyntaxList` → `SeparatedList()`.
- Getting an `InvalidCastException` during tree traversal means that some of the other `VisitXXX` methods returned something unexpected.  
  For example, visiting a `BinaryOperationExpression` and returning some `StatementSyntax` throws this.
- Semantic interpretation is a bit obnoxious. For instance, you cannot get the type belonging to some *statement*, instead you need to get it from its corresponding *expression*. It just doesn't walk to the nearest thing returning something non-null, it expects you to know how it works.
- Returning `null` is a nice deletion, but make sure it doesn't have any weird side effects. 
- Compiling primitive types unfortunately need special handling all over the place. Look at `MCMirrorTypes.cs` for the definition and then browse where they're used for a full list.
- Remember to check `Body` versus `ExpressionBody` for method bodies etc.
- `The name 'a.b' does not exist in the current context` Roslyn doesn't automatically resolve namespaces and looks for a class/struct called `a.b` which does not exist.
- Roslyn is very insistent in seeing `-2147483648` as anything but an int. I get it, but it's really annoying.
- (There was something annoying with `SyntaxKind`, but I forgot.)
  
Minecraft gotchas
=================
- `scoreboard players operation /` floors, and `scoreboard players operation %` uses the positive remainder. These are --hopefully-- only a problem one time, however.

Known issues that are too low priority to fix
=============================================
- Code of the form `label: MyType multiple = 3, declarations = 4;` does not scope correctly and thus leads to incorrect programs.
  - The issue is that `SplitDeclarationsRewriter` returns a block implementing the statements which replaces the single statement. This block should encompass the rest of the scope, but it doesn't.
  - Detecting this is fairly non-trivial, unfortunately, so it won't get a FTxxxx.
  - Normal users barely use goto and also barely use multiple declarations, so this won't happen in serious code.
- Constructors of the form `MyType(args) => expressionStatement;` throws.
  - The issue is that currently `ArrowRewriter` thinks "hey this has a type it must return a value". Constructors don't return, however.
  - Detecting this is trivial but I can't be bothered.
  - Normal users usually won't have a constructor with just one assignment written that way, so this can definitely wait.