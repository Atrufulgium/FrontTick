using static MCMirror.Internal.CompileTime;
using static MCMirror.Internal.RawMCFunction;

namespace System {
    public struct Int32 {

        public static int MaxValue = 2147483647;
        public static int MinValue = -2147483648; // Trivia: if there's no UInt32 definition anywhere, this throws a CS0518. The same does *not* hold for -2147483647. WHy.

        public static int operator +(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ += {VarName(b)} _");
            return res;
        }

        public static int operator +(int a) {
            return a;
        }

        public static int operator -(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ -= {VarName(b)} _");
            return res;
        }

        public static int operator -(int a) {
            return 0 - a;
        }

        public static int operator *(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ *= {VarName(b)} _");
            return res;
        }

        /// <summary>
        /// For positive results, this is the same as
        /// <paramref name="a"/>/<paramref name="b"/>. For negative results
        /// that weren't rounded, it is also the same. For rounded negative
        /// results, this is one lower than regular integer division.
        /// </summary>
        public static int FloorDiv(int a, int b) {
            int res = a;
            Run($"scoreboard players operation {VarName(res)} _ /= {VarName(b)} _");
            return res;
        }

        public static int operator /(int a, int b) {
            // This would be easy... If not for the fact that
            // - c# rounds towards zero (standard);
            // - mcfunction rounds down (ever since 18w32a, see MC-135431).
            // Since I'm aiming for compatability with c#, I need to handle this.
            // Either consider everything positive and handle the sign
            // separately, or do a check with mod. Either way sucks.
            int res = FloorDiv(a,b);
            if (res < 0) {
                if (a % b != 0)
                    res += 1;
            }
            return res;
        }

        /// <summary>
        /// For positive <paramref name="a"/>, this is the same as
        /// <paramref name="a"/>%<paramref name="b"/>. For negative <paramref name="a"/>
        /// however, we take the positive modulus, instead of keeping the sign
        /// like the regular modulo operator.
        /// </summary>
        public static int PositiveMod(int a, int b) {
            int res = a;
            Run($"scoreboard players operation {VarName(res)} _ %= {VarName(b)} _");
            return res;
        }

        public static int operator %(int a, int b) {
            // Minecraft uses *positive* remainder vs c# maintaining sign, apparantly.
            // Also, as per MC-135431 mentioned above, it floors. TODO: Does that matter?
            int res = PositiveMod(a, b);
            if (a < 0 & res != 0)
                res -= b;
            return res;
        }

        public static int operator ~(int a) => -1 - a;

        // Fills the created gap with sign bits
        public static int operator >>(int a, int b) {
            b = PositiveMod(b, 32); // This is part of the spec and I don't like it.
            bool startedNegative = a < 0;
            if (b >= 16) {
                a /= 65536;
                b -= 16;
            }
            if (b >= 8) {
                a /= 256;
                b -= 8;
            }
            if (b >= 4) {
                a /= 16;
                b -= 4;
            }
            if (b >= 2) {
                a /= 4;
                b -= 2;
            }
            if (b >= 1) {
                a /= 2;
            }
            // We're done for positive values, but negative values are off due
            // to the whole "two's complement" thing.
            if (startedNegative)
                a -= 1;
            return a;
        }

        // Fills the created gap with 0 bits.
        public static int operator >>>(int a, int b) {
            b = PositiveMod(b, 32);
            // The first step from negative to positive is different.
            // After that everything's positive and we don't need to care.
            if (b > 0 & a < 0) {
                a = BitHelpers.FlipOnlySignBit(a / 2) - 1;
                b -= 1;
            }
            if (b >= 16) {
                a /= 65536;
                b -= 16;
            }
            if (b >= 8) {
                a /= 256;
                b -= 8;
            }
            if (b >= 4) {
                a /= 16;
                b -= 4;
            }
            if (b >= 2) {
                a /= 4;
                b -= 2;
            }
            if (b >= 1) {
                a /= 2;
            }
            return a;
        }

        // Fills the created gap with 0 bits.
        public static int operator <<(int a, int b) {
            b = PositiveMod(b, 32);
            if (b >= 16) {
                a *= 65536;
                b -= 16;
            }
            if (b >= 8) {
                a *= 256;
                b -= 8;
            }
            if (b >= 4) {
                a *= 16;
                b -= 4;
            }
            if (b >= 2) {
                a *= 4;
                b -= 2;
            }
            if (b >= 1) {
                a *= 2;
            }
            return a;
        }

        public static int operator &(int a, int b) {
            bool aNeg = a < 0;
            bool bNeg = b < 0;
            if (aNeg) {
                a = BitHelpers.FlipOnlySignBit(a);
            }
            if (bNeg) {
                b = BitHelpers.FlipOnlySignBit(b);
            }
            int result = 0;
            int power = 1;
            // 3 bits at a time, 10x.
            // Each iteration, look at the bottom 3 bits and afterwards
            // shift over.
            int i = 0;
            for (; i < 10; i += 1) {
                result += BitHelpers.And8(a % 8, b % 8) * power;
                power *= 8;
                a /= 8;
                b /= 8;
            }
            // Two bits left: MSB and sign (that got removed earlier)
            if (a * b > 0)
                result += power;
            if (aNeg & bNeg)
                result = BitHelpers.FlipOnlySignBit(result);
            return result;
        }

        public static int operator |(int a, int b) => ~(~a & ~b);

        public static int operator ^(int a, int b) {
            // Exactly the same logic as &.
            bool aNeg = a < 0;
            bool bNeg = b < 0;
            if (aNeg) {
                a = BitHelpers.FlipOnlySignBit(a);
            }
            if (bNeg) {
                b = BitHelpers.FlipOnlySignBit(b);
            }
            int result = 0;
            int power = 1;
            int i = 0;
            for (; i < 10; i += 1) {
                result += BitHelpers.Xor8(a % 8, b % 8) * power;
                power *= 8;
                a /= 8;
                b /= 8;
            }
            if (a + b == 1)
                result += power;
            if (aNeg ^ bNeg)
                result = BitHelpers.FlipOnlySignBit(result);
            return result;
        }

        public static bool operator ==(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator !=(int a, int b) {
            bool res;
            res = false;
            Run($"execute unless score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        // TODO: Constant branch using "matches" for these instead of "OP XXX _".
        public static bool operator <=(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ <= {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator <(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ < {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator >=(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ >= {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator >(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ > {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        /// <summary>
        /// Returns the absolute value of <paramref name="a"/>.
        /// </summary>
        public static int Abs(int a) {
            if (a < 0)
                return -a;
            return a;
        }

        /// <summary>
        /// Returns <paramref name="value"/> if it lies between <paramref name="min"/>
        /// and <paramref name="max"/>, or one of those two values if it lies
        /// beyond that boundary.
        /// </summary>
        public static int Clamp(int value, int min, int max)
            => Min(Max(value, min), max);

        /// <summary>
        /// Returns the maximum of two values.
        /// </summary>
        public static int Max(int a, int b) {
            int res = a;
            Run($"scoreboard players operation {VarName(res)} _ > {VarName(b)} _");
            return res;
        }

        /// <summary>
        /// Returns the minimum of two values.
        /// </summary>
        public static int Min(int a, int b) {
            int res = a;
            Run($"scoreboard players operation {VarName(res)} _ < {VarName(b)} _");
            return res;
        }

        /// <summary>
        /// Swaps the values of <paramref name="a"/> and <paramref name="b"/>
        /// in-place.
        /// </summary>
        public static void Swap(ref int a, ref int b) {
            Run($"scoreboard players operation {VarName(a)} _ >< {VarName(b)} _");
        }
    }
}
