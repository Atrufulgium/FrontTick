namespace System {
    // Those annoying bitwise operators need a bunch of helpers.
    public static class BitHelpers {

        public static int FlipOnlySignBit(int num) {
            return int.MinValue + num;
        }

        // A lookup table is slightly quicker, but generates *tons* of files.
        // This is slightly slower but only has fewer auxiliary files.
        /// <summary>
        /// Computes <paramref name="a"/>&amp;<paramref name="b"/> when the
        /// arguments are both in 0, .., 7, and undefined otherwise.
        /// </summary>
        public static int And8(int a, int b) {
            int a4 = int.PositiveMod(a, 4);
            int b4 = int.PositiveMod(b, 4);
            int res = int.PositiveMod(9, a4 * b4 + 1);
            if (a4 == b4)
                res = a4;
            if (a4 < a)
                if (b4 < b)
                    res += 4;
            return res;
        }

        /// <summary>
        /// Computes <paramref name="a"/>^<paramref name="b"/> when the
        /// arguments are both in 0, .., 7, and undefined otherwise.
        /// </summary>
        public static int Xor8(int a, int b) {
            int a4 = int.PositiveMod(a, 4);
            int b4 = int.PositiveMod(b, 4);
            int res = int.PositiveMod(177, a4 + b4 + 3);
            if (a4 == b4)
                res = 0;
            if (a4 < a ^ b4 < b)
                res += 4;
            return res;
        }
        #region ooh (very ad-hoc) math, scary
        // By the way, if you wonder why the above works, recall how sometimes
        // you use an int as a bit(s) array, because that's nice and fast and
        // takes up no space.
        // This is similar, but instead of reading a value from a number with
        // `&` and `>>`, you read a value by, in essence, just `%`, because
        // there's nothing else in Minecraft.
        //
        // --------------------------------------------------------------------
        // UPDATE: I changed the way the methods work. The fundamental idea of
        // "store in modulo" still holds, but the `f` map of step 2 can also be
        // a map f(a,b) where you then try to get it to work on the 4x4 case.
        // Also note we do 4x4 and then can extend it *once* by using
        // inequalities for cheaper than the overhead per bit of the for loop
        // that calls these.
        // For both operators the diagonals make things very difficult, so
        // handle those separately.
        // --------------------------------------------------------------------
        //
        // To construct code for `a OP b` like this, you have to:
        // - STEP 1: Write the entire operator table out, and remove any
        //           symmetries. Try to get rid of as much as possible.
        // - STEP 2: Try a map f(b): [8] → ℕ that is coprime *enough* to...
        // - STEP 3: Try to solve the system { x mod f(b) = a OP b | b } for
        //           each a. Use the chinese remainder theorem. If the system
        //           is not solvable, your f(b) are not coprime enough; try
        //           again with a different map.
        // - STEP 4: If the universe is fair and your OP is symmetric enough
        //           there ought to be tons of structure in the resulting
        //           congruence classes, exploit that.
        //
        // Yeah, both steps 2 and 4 aren't very concrete, I'm sorry.
        //
        // For instance, for &, we follow the steps as follows.
        // - STEP 1: We get the following table
        //           a\b  4 5 6 7
        //           0    0 0 0 0
        //           1    0 1 0 1
        //           2    0 0 2 2
        //           3    0 1 2 3
        //           4    4 4 4 4
        //           5      5 4 5
        //           6        6 6
        //           7          7
        //           With symmetries "a&b = b&a" and "a&b = (a+4)&b for a<4".
        // - STEP 2: Take f(b) to map [8] to (5,6,7,8,5,6,7,8).
        // - STEP 3: We solve x mod f(b) = a&b for all a to get:
        //           a=0:   0 mod 840
        //           a=1: 385 mod 840
        //           a=2: 450 mod 840
        //           a=3: 835 mod 840
        //           a=4:   4 mod 840
        //           a=5:  53 mod 168
        //           a=6:   6 mod  56
        //           a=7    7 mod   8
        // - STEP4: We now choose a sequence of "good" representatives for
        //          these 8 values:
        //          (   0,  385, -400,  -15,    4,  389, -396,  -11)
        //          Δ: (+385, -775, +385,  +19, +385, -775, +385)
        //          where I've already suggestively wrote down the differences.
        //
        //          By the way, in my notebook I write the above process
        //          compactly as follows, makes it easier to try a bunch:
        //           a\%  5 6 7 8     Solutions     Repr     Δ
        //           0    0 0 0 0   {  0 mod 840}      0
        //           1    0 1 0 1   {385 mod 840}    385   +385
        //           2    0 0 2 2   {450 mod 840}   -400   -775
        //           3    0 1 2 3   {835 mod 840}    -15   +385
        //           4    4 4 4 4   {  4 mod 840}      4   -775+794
        //           5      5 4 5   { 53 mod 168}    389   +385
        //           6        6 6   {  6 mod  56}   -396   -775
        //           7          7   {  7 mod   8}    -11   +385
        //
        //          Now, if you consider us as living in ℤ/1160ℤ, six of
        //          these are the same thing; the only outlier is the middle
        //          which needs some special handling.
        //
        //          This saves a ton of branching, but of course working in two
        //          completely different modulus classes at the same time is
        //          not very well-defined, so there's a few hacks sprinkled on
        //          top in the `And8` implementation above.
        //
        // I hope there's a better way to do this though. It *seems* like there
        // should be.
        //
        // Oh and btw I also tried 4-bit &. That ended up with every step being
        // a +393'415 in ℤ/8'934'233ℤ with three corrections:
        //   +6'815'406 from a=3 to aa4;
        //     -418'876 from a=7 to a=8;
        //     -418'876 from a=11 to a=12
        // with modulo map [16] ↦ (1,3,5,7,11,17,23,29,repeat). (Ugly!)
        // This turned out to be less efficient per bit than the 3 bit map
        // above. It seems like 3 bits is the sweet spot for the chinese
        // remainder theorem to not fail for "easy" f(b) maps.
        //
        // Note that as in general you work mod Π_b f(b) and inside ints, that
        // product must be less than Int32.MaxValue, so what values you can
        // choose for f(b) becomes very constrained at 4 bits. 5 bits is right
        // out, unless you have *a LOT* of symmetry to not really make it 5 bits.
        #endregion
    }
}
