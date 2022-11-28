using static MCMirror.Internal.CompileTime;
using static MCMirror.Internal.RawMCFunction;

namespace MCMirror.Internal.Primitives {
    /// <summary>
    /// A fully .mcfunction implementation of <see cref="int"/>.
    /// </summary>
    /// <remarks>
    /// You do not have to type stuff as <tt>MCInt</tt> yourself. The compiler
    /// replaces all instances of <tt>int</tt> with <tt>MCInt</tt>.
    /// </remarks>
    // For my convenience: https://learn.microsoft.com/en-us/dotnet/api/system.int32
#pragma warning disable CS0660, CS0661 // It wants Object.Equals(object o) and Object.GetHashCode() overridden. Not happening.
    public struct MCInt {
#pragma warning restore CS0660, CS0661
        // TODO: attribute or hardcode to *not* convert this int to MCInt.
        int val;
        // TODO: Make constants less awkward.
        // TODO: adding constants is also borked as 2+2 expects the existence
        // of an int +(int,int).
        // TODO: Allow overloads

        // TODO: Handle compile-time constantness and constant folding.
        // (Though, not much needed for compile-time constants as those are
        //  just variables too.)
        ///// <summary>
        ///// This cast exists purely to make constants less awkward in the
        ///// implementation here. In "regular" code, the two types shouldn't
        ///// actually both be used.
        ///// </summary>
        //public static implicit operator MCInt(int i) {
        //    MCInt res;
        //    res.val = i;
        //    return res;
        //}

        public static MCInt operator +(MCInt a, MCInt b) {
            Run($"scoreboard players operation {VarName(a.val)} _ += {VarName(b.val)} _");
            return a;
        }

        public static MCInt operator -(MCInt a, MCInt b) {
            Run($"scoreboard players operation {VarName(a.val)} _ -= {VarName(b.val)} _");
            return a;
        }

        public static MCInt operator *(MCInt a, MCInt b) {
            Run($"scoreboard players operation {VarName(a.val)} _ *= {VarName(b.val)} _");
            return a;
        }

        public static MCInt operator /(MCInt a, MCInt b) {
            Run($"scoreboard players operation {VarName(a.val)} _ /= {VarName(b.val)} _");
            return a;
        }

        public static MCInt operator %(MCInt a, MCInt b) {
            Run($"scoreboard players operation {VarName(a.val)} _ %= {VarName(b.val)} _");
            return a;
        }

        //public static MCInt operator +(MCInt a) {
        //    return a;
        //}

        //public static MCInt operator -(MCInt a) {
        //    MCInt zero;
        //    zero.val = 0;
        //    return zero - a;
        //}

        public static MCInt operator ~(MCInt a) {
            MCInt negone;
            negone.val = -1;
            return negone - a;
        }

        public static MCInt operator &(MCInt a, MCInt b) {
            // TODO
            return a;
        }

        public static MCInt operator |(MCInt a, MCInt b) {
            a = ~a;
            b = ~b;
            a &= b;
            //return ~((~a) & (~b));
            return ~a;
        }

        public static MCInt operator ^(MCInt a, MCInt b) {
            // TODO
            return a;
        }

        public static MCInt operator <<(MCInt a, MCInt b) {
            // TODO
            return a;
        }

        public static MCInt operator >>(MCInt a, MCInt b) {
            // TODO
            return a;
        }

        public static MCInt operator >>>(MCInt a, MCInt b) {
            //TODO
            return a;
        }

        // TODO: Custom behaviour for MCInt where in conditionals, a
        //          if (a == b) { .. }
        // turns into
        //          execute if ... run ...
        // instead of calling this, for all comparison ops.
        public static MCInt operator ==(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute if score {VarName(a.val)} _ = {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }

        public static MCInt operator !=(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute unless score {VarName(a.val)} _ = {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }

        public static MCInt operator <=(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute if score {VarName(a.val)} _ <= {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }

        public static MCInt operator >=(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute if score {VarName(a.val)} _ >= {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }

        public static MCInt operator <(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute if score {VarName(a.val)} _ < {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }

        public static MCInt operator >(MCInt a, MCInt b) {
            MCInt c;
            c.val = 0;
            Run($"execute if score {VarName(a.val)} _ > {VarName(b.val)} _ run scoreboard players set {VarName(c.val)} _ 1");
            return c;
        }
    }
}
