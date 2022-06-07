(To be made neat and separated into properly different files later.)

<!-- omit in toc -->
Table of Contents
======
- [Notes to keep in mind](#notes-to-keep-in-mind)
- [Basic ideas for transformation rules](#basic-ideas-for-transformation-rules)
  - [Basic arithmetic](#basic-arithmetic)
  - [Function calls](#function-calls)
  - [Control flow](#control-flow)
  - [`ref`, `in`, and `out` (for structs)](#ref-in-and-out-for-structs)
  - [Structs (and static objects)](#structs-and-static-objects)
  - [Static arrays of compile-time constant size](#static-arrays-of-compile-time-constant-size)
  - [Non-static non-Minecraft objects](#non-static-non-minecraft-objects)
  - [Minecraft objects](#minecraft-objects)
  - [Throw](#throw)

Notes to keep in mind
======
* Minecraft function names must be `[a-z0-9/._-]`.
* `execute as <selector> run function <function>` calls `function` once for each matched `selector`. This massively increases the number of executed commands, but is likely still faster than not being able to use `@s` everywhere.
* No recursion. Storing the locals of a method `Namespace.Class.Function.local` is easy enough as `namespace#class#function#local`, but breaks when adding recursion. For brevity I omit the `namespace#class#` in the examples below, except where relevant.
* Minecraft's fractional stuff is stupid as NBT-read/writes have that "multiply" factor and scoreboard supports only ints. I'm 50/50 on whether to introduce a `mcfloat` struct to represent "this is a value `* 10^n` or `* 2^n`" for fixed `n` not configurable, or reimplement floating point operations manually entirely. See also [this](https://www.cs.uaf.edu/courses/cs441/notes/floating-point-circuits/). Fixed point with 2 or 3 or compile-time-configurable-amount-of decimal digits may be the way to go as that "multiply" factor needs to know the exponent. This can be done easily with compile time, and less easily with 16 copies of whatever NBT writes we do.
* Supported keywords: `bool` `break` `case` `class` `const` `continue` `do` `else` `enum` `false` `float` `for` `foreach`(?) `goto` `if` `in` `int` `namespace` `new` `null` `operator` `out` `private` `public` `ref` `return` `static` `string` `struct` `switch` `this` `throw` `true` `void` `while`. In other words, basic control flow and single classes without fancy inheritance. Also, `in`, `out`, and `ref`. (TODO: `enum` `float` `foreach` `string`)
* Custom items/mobs in the higher level framework later will use pre-defined allowed inheritance/interfaces -- wanna make a custom item? Implement `IItem`. Wanna make a zombie into a custom mob? Inherit `Zombie`.
* Don't know how minecraft handles it, but it can't hurt to allow an option to minimise the variables. Also we're using only one scoreboard -- `dummy _` -- because that suffices.
* Forgot this was a thing, but try to circumvent NBT as much as possible (by e.g. predicates). Any NBT modification uses the expensive process `save entity -> load from disk -> modify -> save to disk -> load entity`. By using tree search, we can do arrays in $\mathcal O(\log n)$ time *without* NBT modifications.
* For custom items: store properties in stored enchantments? That is checkable in predicates without NBT, does not give enchantment glow, and is unusable on non-enchanted-books. While unfortunately you cannot define custom enchantments ids (minecraft pls), you can use all existing ones, giving quite a lot of possible data to store without needing NBT.
* As per [this post](https://old.reddit.com/r/MinecraftCommands/comments/nw90u4/does_using_predicates_in_place_of_complicated/h18lr5u/), minecraft's selectors are short-circuited. Some are costly. The order from least to most costly is `type (regular) < gamemode < team < type (negated) < tag < name < scores < advancements < nbt`. These are checked in left-to-right order in the selector. Before that, though, comes `level` `x_rotation` `y_rotation` `distance` `x/y/z` `dx/dy/dz` `sort` `limit`.
* Keep track of the worst-case number of commands as metadata in the AST. Give a warning if this exceeds the 100k(/second) treshold.
* Strings are *not* necessarily compile-time constants! You can [save and load for later](https://old.reddit.com/r/MinecraftCommands/comments/g61sc3/how_does_execute_store_result_storage_work_and/fo8ap1w/) their content. (It is also really versatile -- `/tellraw @s {"nbt":"Attributes[0].Base","entity":"@e"}` prints a number for each entity with spaces between.)
* [This post](https://old.reddit.com/r/MinecraftCommands/comments/kjy674/are_predicates_more_efficient_than_nbt_selectors/) shows that predicates are preferable over target selectors (in the nbt case, at least).

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

`ref`, `in`, and `out` (for structs)
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

The other two (`in` and `out`) are just `ref` with some checks. The compiler should liberally add the `in` modifier where possible.

Structs (and static objects)
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
This generalizes naturally to also structs containing structs. To now use the `LengthSquared()`, we call it (as a static method) with its struct as argument.
```csharp
int LengthSquared() {
    return x*x + y*y;
}
// Somewhere else
static void Test() {
    int2 vector;
    int a = vector.LengthSquared();
}
```
⇓
```csharp
static int LengthSquared(int2 instance) {
    return instance.x*instance.x + instance.y*instance.y;
}
// Somewhere else
static void Test() {
    int2 vector;
    int a = int2.LengthSquared(vector);
}
```
⇓
```csharp
static int LengthSquared(int x, int y) {
    return x*x + y*y;
}
// Somewhere else
static void Test() {
    int2 vector;
    int a = int2.LengthSquared(vector.x, vector.y);
}
```
This approach can also be used for any other place where the struct is used as argument. Static objects can be approached the same way as they don't need a heap to be allocated on during runtime.

Static arrays of compile-time constant size
------
The consensus for arrays seems to be to do something with [NBT arrays](https://www.minecraftforum.net/forums/minecraft-java-edition/redstone-discussion-and/commands-command-blocks-and/2961135-1-14-creating-and-using-custom-nbt-arrays-with). However, this gives $\mathcal O(n)$ access time (with NBT, and NBT is slow). Instead, use the $\mathcal O(\log n)$ option of binary search for random accesses:
```csharp
someArray[i] = 42;
```
⇓
```csharp
switch(i) {
    case 0: someArray[0] = 42; break;
    case 1: someArray[1] = 42; break;
    case 2: someArray[2] = 42; break;
    // etc.
    case N: someArray[N] = 42; break;
}
```
Note that both the setter and getter result in `2N` files being generated. Depending on how datapack functions are parsed and ran, this may have impact on general performance.

To store static arrays, use the format `variable#index` as if it were a regular variable, or, if the type is larger than int, `variable#index#fragment`. For instance:
```csharp
static int[] numbers = new int[3] {2,4,8};
```
⇓
```mcfunction
scoreboard players set numbers#0 _ 2
scoreboard players set numbers#1 _ 4
scoreboard players set numbers#2 _ 8
```

```csharp
static int2[] numbers = new int2[3] { new int2(2,4), new int2(3,9), new int2(5,25) };
```
⇓
```mcfunction
scoreboard players set numbers#0#0 _ 2
scoreboard players set numbers#0#1 _ 4
scoreboard players set numbers#1#0 _ 3
scoreboard players set numbers#1#1 _ 9
scoreboard players set numbers#2#0 _ 5
scoreboard players set numbers#2#1 _ 25
```

Non-static non-Minecraft objects
------
Objects are stored on the heap, and there is no way around this -- so we introduce a static heap array. After this things are natural -- references are simply the index in that array, fields and properties are simply offsets within that array, and it takes up `sizeof(object)/4` space. However, as getting things from arrays is $\mathcal O(\log n)$, whenever doing modifications, get the value, mess with it, and then set the updated value only once we're done instead of modifying them directly.

As such, this involves the `4HEAPSIZE` files. To keep this at least somewhat managable, limit the heap to like 2kb or something like that. You're working with an at best ~100khz processor, so this should be plenty.

We do not use fragments as described in the [Static arrays](#static-arrays) section -- this would imply copying over perhaps a *lot* of data (the size of the largest possible object) even for small offsets, which would be bad for small objects. However, in turn this approach is bad for large objects by a factor of $\mathcal O(\log$ `heap size`$)$.

```csharp
public class ReferenceInt2 {
    public int x;
    public int y;
    public ReferenceInt2 RecursionJustForFun;
}
// Later
public void Test() {
    ReferenceInt2 vector = new ReferenceInt2();
    vector.x = 3;
    vector = new ReferenceInt2();
}
```
⇓
```csharp
public void Test() {
    int vectorReference = Allocate(2);
    Heap[vectorReference] = 0;
    Heap[vectorReference + 1] = 0;
    Heap[vectorReference + 2] = NULL;
    Heap[vectorReference] = 3;
    SomethingWithReferenceCounting(vectorReference);
    vectorReference = Allocate(2);
    Heap[vectorReference] = 0;
    Heap[vectorReference + 1] = 0;
    Heap[vectorReference + 2] = NULL;
}
// Somewhere else -- basically have to reimplement the heap entirely with just globals. Fun.
public static int[] Heap = new int[512];
public static int NULL = -1;
public static int Allocate(int size); // (Size in int-units, not byte-units)
public static int GC(); // Or DefragHeap() or something.
public static int SomethingWithReferenceCounting(int ref);
```
Checking null is simple: simply check whether the reference is `-1`. Allocate may return `NULL` if it cannot find any space.

Reference counting is simple enough if we forbid cyclic references. Otherwise we might need to use weak references. However, note that cyclic references are mainly useful in recursion contexts, which we already forbade, so this might be the way to go.

Minecraft objects
------
Directly link writing to NBT, probably. Reading (mainly for booleans but also when there are few options) can maybe be simplified with predicates. Custom item data is easy as that gets serialized with the entity. Custom entity data can be done with markers and passengers, or with the offhand's stored enchantments tag. For instance, the following passes.
```mcfunction
# Set:
data modify entity @e[type=!player,limit=1] HandItems[1] set value {id:"minecraft:stone",Count:1b,tag:{StoredEnchantments:[{lvl:2,id:"backtick:doei"},{lvl:1,id:"backtick:hoi"}]}}
# Test:
execute if entity @e[nbt={HandItems:[{},{tag:{StoredEnchantments:[{id:"backtick:doei",lvl:2}]}}]}] run say Passed!
```
If the entity has no offhand items, simply give it something with CustomModeldata scale 0, and drop chance 0. If the entity changes offhand items, store this data in storage before the change and load it after the change.

For booleans, Tags are preferable as NBT is slow as mentioned numerous times.

Throw
------
Simply print to chat, run a eternal loop from there on out, and prevent further execution. No try/catch/finally support as we don't have stack frames lol.
