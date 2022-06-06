(To be made neat and separated into properly different files later.)

<!-- omit in toc -->
Table of Contents
======
- [Notes to keep in mind](#notes-to-keep-in-mind)
- [Basic ideas for transformation rules](#basic-ideas-for-transformation-rules)
  - [Basic arithmetic](#basic-arithmetic)
  - [Function calls](#function-calls)
  - [Control flow](#control-flow)
  - [`ref`](#ref)
  - [Structs](#structs)
  - [Objects](#objects)

Notes to keep in mind
======
* Minecraft function names must be `[a-z0-9/._-]`.
* `execute as <selector> run function <function>` calls `function` once for each matched `selector`. This massively increases the number of executed commands, but is likely still faster than not being able to use `@s` everywhere.
* No recursion. Storing the locals of a method `Namespace.Class.Function.local` is easy enough as `namespace#class#function#local`, but breaks when adding recursion. For brevity I omit the `namespace#class#` in the examples below, except where relevant.
* Minecraft's fractional stuff is stupid as NBT-read/writes have that "multiply" factor and scoreboard supports only ints. I'm 50/50 on whether to introduce a `mcfloat` struct to represent "this is a value `* 10^n` or `* 2^n`" for fixed `n` not configurable, or reimplement floating point operations manually entirely. See also [this](https://www.cs.uaf.edu/courses/cs441/notes/floating-point-circuits/).
* Supported keywords: `bool` `break` `case` `class` `const` `continue` `do` `else` `enum` `false` `float` `for` `foreach`(?) `goto` `if` `in` `int` `namespace` `new` `null` `operator` `out` `private` `public` `ref` `return` `static` `string` `struct` `switch` `this` `true` `void` `while`. In other words, basic control flow and single classes without fancy inheritance. Also, `in`, `out`, and `ref`.
* Custom items/mobs in the higher level framework later will use pre-defined allowed inheritance/interfaces -- wanna make a custom item? Implement `IItem`. Wanna make a zombie into a custom mob? Inherit `Zombie`.
* Don't know how minecraft handles it, but it can't hurt to allow an option to minimise the variables. Also we're using only one scoreboard -- `dummy _` -- because that suffices.

Basic ideas for transformation rules
======

Basic arithmetic
------
```csharp
[MCFunction]
static void Test() {
    int a = 3;
    int b = 7;
    int c = a*a+b+2;
}
```
⇓
```mcfunction
# (File test.mcfunction)
scoreboard players set Test#a _ 3
scoreboard players set Test#b _ 7
scoreboard players operation Test#c _ = Test#a _
scoreboard players operation Test#c _ *= Test#a _
scoreboard players operation Test#c _ += Test#b _
scoreboard players operation Test#c _ += #CONSTS#2 _
```

Function calls
------
No need to mark called methods as `[MCFunction]` as this is derived automatically. The attribute is purely intended for entrypoints you use with manual commandblocks or commands. If not inlined, those called methods get put into a deeper folder out of view (`internal/*` or something). All methods marked `[MCFunction]` must be of the signature `static void MethodName()`.

For any function `<type> Method(<type> a, <type> b)` it is the responsibility of the call site to fill in the arguments.

Small function call (and for later: in a context with few selectors):
```csharp
[MCFunction]
static void Test() {
    int a = 3;
    a = Calculate(a);
}
static int Calculate(int a) {
    return a * a;
}
```
⇓
```mcfunction
# (File test.mcfunction)
scoreboard players set Test#a _ 3
scoreboard players operation Test#a _ *= Test#a _
```

Large function call (and for later: also in contexts with many selectors):
```csharp
[MCFunction]
static void Test() {
    int a = 3;
    a = Calculate(a);
}
static int Calculate(int a) {
    // [Large calculation]
    return a * a;
}
```
⇓
```mcfunction
# (File say.mcfunction)
scoreboard players set Test#a _ 3
scoreboard players operation Calculate#a _ = Test#a _
function namespace:internal/calculate
scoreboard players operation Test#a _ = #RET _

# (File internal/calculate.mcfunction)
# [Large calculation]
scoreboard players operation Calculate#a _ *= Calculate#a _
scoreboard players operation #RET _ = Calculate#a _
```

Control flow
------
All control flow must result in something that can be determined via `<`, `>`, `<=`, `>=`, `=`, (for comparing with other variables) or a `range` (for constants). The range parameter in particular is very neat.

Note: there are also other `execute if ...` variants. There is `block` that tests a literal block, there is `blocks` that compares two cuboid regions (including nbt! so you can check whether e.g. chests match), there is `data` that checks for the existence of NBT in blocks/entity/storage, `entity` which checks for the existence of entities, and `predicate` which is stupidly broad and needs proper wiki-reading and testing.
```csharp
static int Abs(int a) {
    if (a < 0)
        a = -a;
    return a;
}
```
⇓
```mcfunction
# (File internal/abs.mcfunction)
execute if score Abs#a _ matches ..-1 run scoreboard players operation Abs#a _ *= #CONSTS-1 _
scoreboard players operation #RET _ = Abs#a _
```

Note that booleans are implemented simply as integers via the standard `false=0` convention.
```csharp
if (Abs(a) > 2 && a > -3)
    a = 0;
```
⇓
```csharp
bool result = Abs(a) > 2 && a > -3;
if (result)
    a = 0;
```

```csharp
static int Test(int a) {
    if (a < -1)
        a = Abs(a);
    return a;
}
```
⇓
```mcfunction
# (File internal/test.mcfunction)
execute if score Test#a _ matches ..-2 run function namespace:internal/test.branch1.mcfunction
scoreboard players operation #RET _ = Test#a

# (File internal/test.branch1.mcfunction)
scoreboard players operation Abs#a _ = Test#a _
function namespace:internal/abs
scoreboard players operation Test#a _ = #RET _
```
Note that the final `Test#a = #RET` is superfluous as the next line (in `internal/test.mcfunction`) is `#RET = Test#a`. This is a relatively rare case low on the todo list.

```csharp
if (a > 0) {
    // Stuff
} else {
    // Stuff
}
```
⇓
```csharp
if (a > 0) {
    // Stuff
}
if (!a > 0) {
    // Stuff
}
```

```csharp
if (a > 0) {
    // Stuff
} else if (b > 0) {
    // Stuff
} else {
    // Stuff
}
```
⇓
```csharp
if (a > 0) {
    // Stuff
} else {
    if (b > 0) {
        // Stuff
    } else {
        // Stuff
    }
}
```

`goto` will be fundamental to the other control flows that aren't switch. (Note that `goto` also is allowed in switches with constant cases, so todo: keep that in mind.)
It works by simply extracting everything after the label in the same scope into its own file and calling that immediately. Then also call that whenever encountering that `goto`.
```csharp
static int Test(int a) {
    a += 1;
Label:
    a += 2;
    goto Label;
}
```
⇓
```mcfunction
# (File internal/test.mcfunction)
scoreboard players operation Test#a _ += #CONSTS#1 _
function namespace:test.goto_label.mcfunction

# (File internal/test.goto_label.mcfunction)
scoreboard players operation Test#a _ += #CONSTS#2 _
function namespace:test.goto_label.mcfunction
```

```csharp
while(condition) {
    // Stuff
}
```
⇓
```csharp
LabelN:
if (condition) {
    // Stuff
    goto LabelN;
}
LabelBreakN: // Only if there is a break-statement in "Stuff"
```

```csharp
do {
    // Stuff
} while (condition);
```
⇓
```csharp
LabelN:
// Stuff
if (condition)
    goto LabelN;
LabelBreakN: // Only if there is a break-statement in "Stuff"
```

```csharp
for(init; condition; incr) {
    // Stuff
}
```
⇓
```csharp
init;
while(condition) {
    //Stuff
    incr;
}
```

```csharp
break;
```
⇓
```csharp
goto LabelBreakN;
```

```csharp
continue;
```
⇓
```csharp
goto LabelN;
```

The switch statement is interesting. There are no jump tables possible, so simply sort the cases and binary search-ish which case we are in. "Simply sort the cases" is a major assumption, but we can do that for all datatypes that we care about. This gives, for instance:
```csharp
switch(int a) {
    case 3:
    case 4:
    case 5:
        // Stuff 1
        break;
    case 19:
    case 210:
        // Stuff 2
        break;
    default:
        // Stuff 3
        break;
}
```
⇓
```csharp
if (a <= 19) {
    if (3 <= a && a <= 5) {
    LabelNCase3_4_5: // If anything goto's to case 3,4,5
        // Stuff 1
    } else {
    LabelNCase19: // If anything goto's to case 19
        // Stuff 2
    }
} else {
    if (a == 210) {
    LabelNCase210: // If anything goto's to case 210
        // Stuff 2
    } else {
    LabelNDefault: // If anything goto's to the default case
        // Stuff 3
    }
}
```

`ref`
------
This is pretty simple. Consider the following code.
```csharp
[MCFunction]
static void Test() {
    int x = 3;
    int y = 4;
    Swap(x,y);
}
static void Swap(ref int a, ref int b) {
    int c = a;
    a = b;
    b = c;
}
```
Where the code, without the refs, would normally use `Swap#a` and `Swap#b`, here we keep using `Test#x` and `Test#y` instead within `Swap`.

Structs
------
So far everything's been just ints, but things can be better than this. Consider the following snippet.
```csharp
public struct int2 {
    public int x;
    public int y;

    int LengthSquared() {
        return x*x + y*y;
    }
}
```
Now when we use this struct in a function, the data gets translated the following way.
```csharp
[MCFunction]
static void Test() {
    int2 vector = new int2() {
        x = 3, y = 4
    };
}
```
⇓
```mcfunction
# (File test.mcfunction)
scoreboard players set Test#vector#x _ 3
scoreboard players set Test#vector#y _ 4
```
This generalizes naturally to also structs containing structs. To now use the `LengthSquared()`, we call it (as a static method) with its struct data as argument.
```csharp
int LengthSquared() {
    return x*x + y*y;
}
```
⇓
```csharp
static int LengthSquared(int x, int y) {
    return x*x + y*y;
}
```
This approach can also be used for any place where the struct is used as argument.

Objects
------
Dunno
