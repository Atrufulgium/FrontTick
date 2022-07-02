This compilation stage expects very specifically formatted code, which is the job of the previous stages. The only supported statements are the following in the left column; the rest raise errors. (So e.g. while loops, etc. at this point!) This stage does the following transformations.

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
<td> ❌ </td>
<td>

Branching with simple args:
```csharp
if (condition) {
    // Code1 - one op
} else {
    // Code2 - many ops
}
```
(Note that Roslyn already expands `else if`.)

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
<td> ❌ </td>
<td>

Switches

</td>
<td>

Oh no. See the brainstorm, but this one's *spicy*. Note that Roslyn does said spice for non-contiguous ranges already so I *may* be able to hack around it. Perhaps just multiply all cases by two?

</td>
</tr>
<tr>
<td> ❌ </td>
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
Labels just are attached `-labelname` to the original function name and do not stack.

</td>
</tr>
<tr>
<td> ❌ </td>
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
The obvious solution consists of checking in all scopes between this and the target whether we `goto`'d via a flag. This explodes (especially in switches etc), but I can't think of anything better.

</td>
</tr>
<tr>
<td> ❌ </td>
<td>

Branch-independent return:
```csharp
// (Method root scope)
return value;
```

</td>
<td>

Simply assign to the special `#RET` (or for larger types `#RET#0`, `#RET#1`, etc.).

</td>
</tr>

<tr>
<td> ❌ </td>
<td>

Branch-dependent return:
```csharp
// (Method root scope)
if (condition)
    return value;
// (More returns)
// (No non-returns)
```
(1) With `return` the *only* statement in the branch. <br/>
(2) No other code non-`return`ing code after.

</td>
<td>

Simply have a conditional assignment to `#RET`. No need to quit the function because there is no code after this anyway. (This state should be achieved with `goto`s.)

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