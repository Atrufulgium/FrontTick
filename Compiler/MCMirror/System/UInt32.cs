namespace System {
    // Note to self: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions
    // TODO: Those conversions seem implicit *even in Roslyn*. The cast methods don't trigger if you write `uint a = 123 You must write `uint a = 123u`...
    [MCMirror.Internal.CompilerUsesName]
    public struct UInt32 {
        /// <summary>
        /// The value this int represents is just what you get with two's complement.
        /// [0,int.MaxValue] are the same, [int.MinValue, -1] represent
        /// [int.MaxValue+1, uint.MaxValue].
        /// </summary>
        int val;

        public UInt32(int underlyingValue) {
            val = underlyingValue;
        }

        public static uint MaxValue = new uint(-1);
        public static uint MinValue = new uint(0);
        public static uint Zero = new uint(0);
        
        public static uint operator +(uint a, uint b) => new uint(a.val + b.val);
        public static uint operator +(uint a) => a;
        public static uint operator -(uint a, uint b) => new uint(a.val - b.val);
        public static uint operator -(uint a) => new(-a.val);
        public static uint operator *(uint a, uint b) => new uint(a.val * b.val);

        public static uint operator /(uint a, uint b) {
            // Do not get confused in these methods with comparisons of uints
            // and comparisons of their value.
            if (a < b)
                return Zero;
            // TODO: How to handle exceptions?
            if (b == Zero)
                return Zero;

            if (a.val >= 0) {
                // Due to "if (a < b) return Zero;" there is no "a small, b
                // large" case, so no need to test on the sign of b.val.
                // a small, b small
                return new uint(int.FloorDiv(a.val, b.val));
            } else {
                if (b.val >= 0) {
                    // a large, b small
                    // (2^31 + TwosComplement(a.val)) / b.val
                    a = Complement(a);
                    // Abbreviated as (2^31 + a)/b:
                    // = a/b + (2^31 + (a%b))/b
                    // = a/b + (2^31 + (a%b) - b)/b + 1
                    // = a/b + (2^31 - (b - a%b))/b + 1
                    // = a/b - (int.MinValue + (b - a%b))/(-b) + 1
                    // In this last line, both /'s are well-defined and no
                    // overflow (resulting in unexpected values) happens.
                    int aDiv = int.FloorDiv(a.val, b.val);
                    int aRem = int.PositiveMod(a.val, b.val);
                    int dividand = int.MinValue + (b.val - aRem);
                    return new uint(aDiv + int.FloorDiv(dividand, -b.val) + 1);
                } else {
                    // a large, b large
                    // Then we're the form (2^31 + a') / (2^31 + b') with both
                    // a' and b' in [0,2^31-1].  So the result is either 0 or 1
                    // but it's actually 1 due to "if (a < b) return Zero;".
                    return new uint(1);
                }
            }
        }

        public static uint operator %(uint a, uint b) {
            if (a < b)
                return a;
            if (b == Zero)
                return Zero;

            if (a.val >= 0) {
                // a small, b small
                return new uint(int.PositiveMod(a.val, b.val));
            } else {
                if (b.val >= 0) {
                    //a large, b small
                    // (2^31 + TwosComplement(a.val)) / b.val
                    a = Complement(a);
                    // Abbreviated as (2^31 + a)%b:
                    // = ((2^31 % b) + a%b)%b
                    // = ((2^31 - b)%b + a%b)%b
                    // = ((-(int.MinValue + b))%b + a%b)%b
                    // In this last line, all %'s are positive and no
                    // overflow (resulting in unxpected values) happens.
                    int innerMod1 = int.PositiveMod(-(int.MinValue + b.val), b.val);
                    int innerMod2 = int.PositiveMod(a.val, b.val);
                    return new uint(int.PositiveMod(innerMod1 + innerMod2, b.val));
                } else {
                    // a large, b large
                    // As in the div case the result is 1, simply take the
                    // difference.
                    return a - b;
                }
            }
        }

        public static uint operator ~(uint a) => new uint(~a.val);
        public static uint operator >>(uint a, int b) {
            b = int.PositiveMod(b, 32);
            // The same if the top bit is not set
            if (a.val >= 0)
                return new uint(a.val >> b);
            // Otherwise, do the first shift manually.
            if (b > 0) {
                a /= 2u;
                b -= 1;
            }
            return new uint(a.val >> b);
        }

        public static uint operator >>>(uint a, int b) => new uint(a.val >>> b);
        public static uint operator <<(uint a, int b) => new uint(a.val << b);
        public static uint operator &(uint a, uint b) => new uint(a.val & b.val);
        public static uint operator |(uint a, uint b) => new uint(a.val | b.val);
        public static uint operator ^(uint a, uint b) => new uint(a.val ^ b.val);
        public static bool operator ==(uint a, uint b) => a.val == b.val;
        public static bool operator !=(uint a, uint b) => a.val != b.val;
        public static bool operator <=(uint a, uint b) => a.val - int.MinValue <= b.val - int.MinValue;
        public static bool operator <(uint a, uint b) => a.val - int.MinValue < b.val - int.MinValue;
        public static bool operator >=(uint a, uint b) => a.val - int.MinValue >= b.val - int.MinValue;
        public static bool operator >(uint a, uint b) => a.val - int.MinValue > b.val - int.MinValue;

        // I'd rather clamp but this is what it does in an unchecked block
        // in vanilla c#.
        public static explicit operator uint(int value) => new uint(value);
        public static explicit operator int(uint value) => value.val;

        public static uint Clamp(uint value, uint min, uint max) => Min(Max(value, min), max);
        public static uint Min(uint a, uint b) {
            if (a <= b)
                return a;
            return b;
        }
        public static uint Max(uint a, uint b) {
            if (a <= b)
                return b;
            return a;
        }
        
        public static bool IsPow2(uint value) {
            if (value.val == 1) return true;
            if (value.val == 2) return true;
            if (value.val == 4) return true;
            if (value.val == 8) return true;
            if (value.val == 16) return true;
            if (value.val == 32) return true;
            if (value.val == 64) return true;
            if (value.val == 128) return true;
            if (value.val == 256) return true;
            if (value.val == 512) return true;
            if (value.val == 1024) return true;
            if (value.val == 2048) return true;
            if (value.val == 4096) return true;
            if (value.val == 8192) return true;
            if (value.val == 16384) return true;
            if (value.val == 32768) return true;
            if (value.val == 65536) return true;
            if (value.val == 131072) return true;
            if (value.val == 262144) return true;
            if (value.val == 524288) return true;
            if (value.val == 1048576) return true;
            if (value.val == 2097152) return true;
            if (value.val == 4194304) return true;
            if (value.val == 8388608) return true;
            if (value.val == 16777216) return true;
            if (value.val == 33554432) return true;
            if (value.val == 67108864) return true;
            if (value.val == 134217728) return true;
            if (value.val == 268435456) return true;
            if (value.val == 536870912) return true;
            if (value.val == 1073741824) return true;
            if (value.val == -2147483648) return true;
            return false;
        }

        public static uint Log2(uint value) {
            if (value.val < 0) return new uint(31);
            // Note that Log2(0u) = 0 in c#.
            if (value.val < 2) return new uint(0);
            if (value.val < 4) return new uint(1);
            if (value.val < 8) return new uint(2);
            if (value.val < 16) return new uint(3);
            if (value.val < 32) return new uint(4);
            if (value.val < 64) return new uint(5);
            if (value.val < 128) return new uint(6);
            if (value.val < 256) return new uint(7);
            if (value.val < 512) return new uint(8);
            if (value.val < 1024) return new uint(9);
            if (value.val < 2048) return new uint(10);
            if (value.val < 4096) return new uint(11);
            if (value.val < 8192) return new uint(12);
            if (value.val < 16384) return new uint(13);
            if (value.val < 32768) return new uint(14);
            if (value.val < 65536) return new uint(15);
            if (value.val < 131072) return new uint(16);
            if (value.val < 262144) return new uint(17);
            if (value.val < 524288) return new uint(18);
            if (value.val < 1048576) return new uint(19);
            if (value.val < 2097152) return new uint(20);
            if (value.val < 4194304) return new uint(21);
            if (value.val < 8388608) return new uint(22);
            if (value.val < 16777216) return new uint(23);
            if (value.val < 33554432) return new uint(24);
            if (value.val < 67108864) return new uint(25);
            if (value.val < 134217728) return new uint(26);
            if (value.val < 268435456) return new uint(27);
            if (value.val < 536870912) return new uint(28);
            if (value.val < 1073741824) return new uint(29);
            return new uint(30);
        }

        /// <summary>
        /// Swaps ranges [0, 2^31-1] and [2^31, 2^32-1].
        /// </summary>
        static int Complement(int value) => value - int.MinValue;
        /// <inheritdoc cref="Complement(int)"/>
        static uint Complement(uint value) => new(Complement(value.val));
    }
}