This compilation stage expects very specifically formatted code, which is the job of the previous stages. The only supported statements are the following in the `Before` column; the rest raise errors. (So e.g. while loops, etc. at this point!) It's subtle in the examples, but note that any block needs to *actually be a block*.

This phase only does few optimisations, namely:
* Turn `execute ... run execute ...` into `execute ... ...` without the extra `run execute`.
* Single-line branches do not branch into a separate file.

TODO: The generated code also needs a few optimisations still. Namely:
* Any mcfunction that is just a single `function ...` can be replaced at callsite with the called function;
* Any mcfunction that is empty can be deleted and its callsite can be removed.
* Multiple `goto`s to the same label generate different files that can easily be shared.
* Branches with an "else" statement store the conditional in a unique temporary variable. Do the analysis of whether this is actually needed; the if-branch may not modify it.

<table>
<tr>
<td> </td> <td> Before </td> <td> After </td>
</tr>
<tr>
<td> ✅ </td>
<td>

Declarations without initialization:
```csharp
int integer;
```

</td>
<td>

Ignored if at root. Only necessary for well-formed c# code. Do not have declarations with initialization. <br/>
If not at the method root scope, throws an error.

</td>
</tr>
<tr>
<td> ✅ </td>
<td>

Integer binary arithmetic assignments, purely scoreboard:
```csharp
thing1 ∘= thing2;
```
(where operator `∘` ∈ {`+`,`-`,`*`,`/`,`%`,`​`})

</td>
<td>

If ∘ is nonempty or `thing2` is not a literal:
```mcfunction
scoreboard players operation TARGET _ ∘= TARGET _
```
Otherwise:
```mcfunction
scoreboard players set TARGET _ = TARGET
```

</td>
</tr>
<tr>
<td> ❌ </td>
<td>

Simple assignments, allowing NBT/blocks/etc:
```csharp
thing1 = thing2;
```

</td>
<td>

The write part is one of:
```mcfunction
execute store result score TARGET _ run ...
execute store result entity ENTITY PATH TYPE SCALE  run ...
execute store result block POSITION PATH TYPE SCALE run ...
```
The read part is one of:
```mcfunction
scoreboard players get TARGET _
data get entity ENTITY PATH SCALE
data get block POSITION PATH SCALE
```
(Player entities excluded.) <br/>
If the RHS is constant, this is simpler:
```mcfunction
scoreboard players set TARGET _ 230
```

</td>
</tr>
<tr>
<td> 🟡 </td>
<td>

Static function calls with non-arithmetic args:
```csharp
Method(in thing1, thing2, out thing3);
```
Note that inlining must happen before. <br/>
This compilation stage never does any inlining.

</td>
<td>

```mcfunction
scoreboard players operation Method#thing2 _ = OtherContext#thing2 _
function namespace:somemethod
```
The `somemethod` mcfunction must be a variant that uses the same variable names as this scope.
<br/> <br/>
*The `in`, `ref`, and `out` keywords are not yet implemented.*

</td>
</tr>
<tr>
<td> 🟡 </td>
<td>

Function calls as above, storing a result anywhere:
```csharp
result = Method();
```
Note that this case includes non-integer arithmetic.

</td>
<td>

Similar preperatory work for the arguments as previous, and similar write-work as a bit more previous.
```mcfunction
# Prep args
function somemethod
# Write, e.g.
execute store result entity ENTITY PATH TYPE SCALE  run scoreboard players get #RET _
```
Note that we store the result of functions in a special player `#RES` (or `#RES#0`, `#RES#1`, etc for larger types). <br/>
This means we can do the write-work exactly as the arbitrary assignment case.
<br/><br/>
*As above, but also only implemented for scoreboard.*

</td>
</tr>
<tr>
<td> ✅ </td>
<td>

Branching:
```csharp
if (identifier != 0) {
    // Code1 - one op
} else {
    // Code2 - many ops
}
```
Allows for `==` and `!=`, with integer literal RHS.
Note that `else if` is disallowed; it should be `else { if }`.

</td>
<td>

If it's one statement, inline it. <br/>
If not, separate the block into a function and call that.
```mcfunction
execute unless score Context#condition _ matches 0 run ...
execute if score Context#condition _ matches 0 run function namespace:context-else-block-1
```

</td>
</tr>
<tr>
<td> 🟡 </td>
<td>

Goto labels:
```csharp
Label:
// Code
```

</td>
<td>

Upon encountering a label, create a new function for the remainder and run it.
```mcfunction
function namespace:method-label1
```
Labels just are attached `-label-number` to the original function name and do not stack.

*Only implemented for "raw(?)" labels, not switch case labels.*

</td>
</tr>
<tr>
<td> ✅ </td>
<td>

Goto statement:
```csharp
goto Label1;
```

</td>
<td>

Just use the method from the previous rule.
```mcfunction
function namespace:method-label1
```
`if (..) goto ..` does *not* prevent execution from the part after the branch finishes. <br/>
The obvious solution consists of checking in all scopes between this and the target whether we `goto`'d via a flag. This explodes (especially in switches etc), but I can't think of anything better, so I went with that.

Do note that the implementation of this needs to *also* deal with code splitting into a bazillion executors due to selectors being able to suddenly fork a ton. I don't even *know* what it is supposed to mean for e.g. a single branch of execution splitting into many and then returning different values.

</td>
</tr>
<tr>
<td> ✅ </td>
<td>

Return, anywhere.
```csharp
return [literal | identifier | call];
```
(Secretly an assignment storing a result in scoreboard.)

</td>
<td>

Simply assign to the special `#RET` (or for larger types `#RET#0`, `#RET#1`, etc.), the same way as integer-scoreboard assignment.

We can ignore any assignment and handle just the call if it is of the form `return Value();` because otherwise we'd get `#RET _ = #RET _`.
</td>
</tr>
<tr>
<td> ❌ </td>
<td>

Anything relating to objects.

</td>
<td>

Objects are not worked out in detail yet.

</td>
</tr>
</table>