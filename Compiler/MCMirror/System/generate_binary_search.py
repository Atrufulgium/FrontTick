"""
Given ranges of input values [(a,b)] with corresponding outputs [(c)],
generates the c# if-else tree that performs said mapping.

To use this file, generate your ranges into `ranges`, outputs into `outputs`
and the checked value into `varname`.

The number of ranges and outputs must be the same, and ranges must be sorted
in ascending order. Ranges may either be a single int value, or a (min,max)
tuple.
"""
import math
from typing import List, Tuple, Union

Range = Union[int,Tuple[int,int]]
int_min = -2147483648
int_max = 2147483647

# CONFIGURE HERE
ranges : List[Range] = [(int_min, -1), (0,0)] + [(2**i, 2**(i+1) - 1) for i in range(0,31)]
output : List[str] = ["new uint(0)", "new uint(32)"] + [f"new uint({i})" for i in range(31, 0, -1)]
varname = "value.val"

def print_csharp(ranges : List[Tuple[int,int]], output : List[str], depth : int = 3) -> str:
    if len(ranges) != len(output):
        raise ValueError("Number of outputs doesn't match number of ranges.")
    if len(ranges) == 0:
        raise ValueError("(impossible branch)")
    
    indent = '\t' * depth
    indent = f"\n{indent}"
    if len(ranges) == 1:
        return f"{indent}return {output[0]};"
    if len(ranges) == 2:
        return f"{indent}if ({varname} < {ranges[1][0]}) return {output[0]};{indent}return {output[1]};"

    split_index = power_two_at_most(len(ranges))
    if split_index == len(ranges):
        split_index //= 2
    else:
        split_index = len(ranges) - split_index # to prioritise less branching for lower values
    ranges_first_half = ranges[:split_index]
    output_first_half = output[:split_index]
    ranges_second_half = ranges[split_index:]
    output_second_half = output[split_index:]
    return (
        f"{indent}if ({varname} < {ranges_second_half[0][0]}) {{"
        + print_csharp(ranges_first_half, output_first_half, depth+1) +
        f"{indent}}} else {{"
        + print_csharp(ranges_second_half, output_second_half, depth+1) +
        f"{indent}}}"
    )

def power_two_at_most(n : int) -> int:
    return 2**int(math.log(n, 2))
    

ranges_tupled = [r if isinstance(r, tuple) else (r,r) for r in ranges]
print(f"Ranges: {ranges_tupled}")
print(f"Output: {output}")
print(print_csharp(ranges_tupled, output, 3))
