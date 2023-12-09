using MCMirror.Internal;

namespace MinecraftTests {
    internal class IntTests {

        // Yeah so primitives are special and *apparantly* this went wrong once.
        [MCTest(-2147483648)]
        public static int TestReadMinValue() {
            return int.MinValue;
        }

        [MCTest(357)]
        public static int TestBasicArith1() {
            return 234 + 123;
        }

        [MCTest(111)]
        public static int TestBasicArith2() {
            return 234 - 123;
        }

        [MCTest(28782)]
        public static int TestBasicArith3() {
            return 234 * 123;
        }

        [MCTest(111)]
        public static int TestBasicArith4() {
            return 234 % 123;
        }

        // Division in minecraft and in mcfunction differs, apparantly.
        [MCTest(1)]
        public static int TestDivision1() {
            return 234 / 123;
        }
        [MCTest(-1)]
        public static int TestDivision2() {
            return -234 / 123;
        }
        [MCTest(-1)]
        public static int TestDivision3() {
            return 234 / -123;
        }
        [MCTest(1)]
        public static int TestDivision4() {
            return -234 / -123;
        }

        [MCTest(-234)]
        public static int TestUnary() {
            return - + - + - +234;
        }

        [MCTest(-235)]
        public static int TestBitNegate() {
            return ~234;
        }

        [MCTest(29)]
        public static int TestRightArithmeticShift1() {
            return 234 >> 3;
        }

        [MCTest(0)]
        public static int TestRightArithmeticShift2() {
            return 234 >> 30;
        }

        [MCTest(234)]
        public static int TestRightArithmeticShift3() {
            return 234 >> 32;
        }

        [MCTest(-30)]
        public static int TestRightArithmeticShift4() {
            return -234 >> 3;
        }

        [MCTest(-1)]
        public static int TestRightArithmeticShift5() {
            return -234 >> 30;
        }

        [MCTest(29)]
        public static int TestRightLogicalShift1() {
            return 234 >>> 3;
        }

        [MCTest(0)]
        public static int TestRightLogicalShift2() {
            return 234 >>> 30;
        }

        [MCTest(234)]
        public static int TestRightLogicalShift3() {
            return 234 >>> 32;
        }

        [MCTest(536870882)]
        public static int TestRightLogicalShift4() {
            return -234 >>> 3;
        }

        [MCTest(3)]
        public static int TestRightLogicalShift5() {
            return -234 >>> 30;
        }

        [MCTest(1872)]
        public static int TestLeftShift1() {
            return 234 << 3;
        }

        [MCTest(-2147483648)]
        public static int TestLeftShift2() {
            return 234 << 30;
        }

        [MCTest(234)]
        public static int TestLeftShift3() {
            return 234 << 32;
        }

        [MCTest(-1872)]
        public static int TestLeftShift4() {
            return -234 << 3;
        }

        [MCTest(-2147483648)]
        public static int TestLeftShift5() {
            return -234 << 30;
        }

        [MCTest(84419208)]
        public static int TestAnd1() {
            return 0b0_10110_11001_11000_11010_10101_01100
                 & 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(680806436)]
        public static int TestAnd2() {
            return 0b0_10110_11001_11000_11010_10101_01100
                & -0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(308446272)]
        public static int TestAnd3() {
            return -0b0_10110_11001_11000_11010_10101_01100
                  & 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-1073671916)]
        public static int TestAnd4() {
            return -0b0_10110_11001_11000_11010_10101_01100
                 & -0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(989252710)]
        public static int TestXor1() {
            return 0b0_10110_11001_11000_11010_10101_01100
                 ^ 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-989252710)]
        public static int TestXor2() {
            return 0b0_10110_11001_11000_11010_10101_01100
                ^ -0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-989252706)]
        public static int TestXor3() {
            return -0b0_10110_11001_11000_11010_10101_01100
                  ^ 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(989252706)]
        public static int TestXor4() {
            return -0b0_10110_11001_11000_11010_10101_01100
                 ^ -0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(1073671918)]
        public static int TestOr1() {
            return 0b0_10110_11001_11000_11010_10101_01100
                 | 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-308446274)]
        public static int TestOr2() {
            return 0b0_10110_11001_11000_11010_10101_01100
                | -0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-680806434)]
        public static int TestOr3() {
            return -0b0_10110_11001_11000_11010_10101_01100
                  | 0b0_01011_10110_10101_01001_10110_01010;
        }

        [MCTest(-84419210)]
        public static int TestOr4() {
            return -0b0_10110_11001_11000_11010_10101_01100
                 | -0b0_01011_10110_10101_01001_10110_01010;
        }

#pragma warning disable CS0162 // Unreachable code detected
        [MCTest(1)]
        public static int TestEquality1() {
            if (3 == 4)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestEquality2() {
            if (3 == 3)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestInequality1() {
            if (3 != 4)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestInequality2() {
            if (3 != 3)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestGreaterThan1() {
            if (3 > 4)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestGreaterThan2() {
            if (3 > 3)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestGreaterThan3() {
            if (3 > 2)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestLessThan1() {
            if (3 < 4)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestLessThan2() {
            if (3 < 3)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestLessThan3() {
            if (3 < 2)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestGreaterThanEquals1() {
            if (3 >= 4)
                return 0;
            return 1;
        }

        [MCTest(1)]
        public static int TestGreaterThanEquals2() {
            if (3 >= 3)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestGreaterThanEquals3() {
            if (3 >= 2)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestLessThanEquals1() {
            if (3 <= 4)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestLessThanEquals2() {
            if (3 <= 3)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestLessThanEquals3() {
            if (3 <= 2)
                return 0;
            return 1;
        }
#pragma warning restore CS0162 // Unreachable code detected

        [MCTest(230)]
        public static int TestAbs1() {
            return int.Abs(230);
        }

        [MCTest(0)]
        public static int TestAbs2() {
            return int.Abs(0);
        }

        [MCTest(230)]
        public static int TestAbs3() {
            return int.Abs(-230);
        }

        [MCTest(1)]
        public static int TestClamp1() {
            return int.Clamp(230, 0, 1);
        }

        [MCTest(230)]
        public static int TestClamp2() {
            return int.Clamp(230, 0, 1000);
        }

        [MCTest(999)]
        public static int TestClamp3() {
            return int.Clamp(230, 999, 1000);
        }

        // "Swap" is already implicitely tested by the exaustive And8/Xor8s.
        // "Min" and "Max" are already implicitely tested by Clamp.
        // "FloorDiv" and "PositiveMod" are tested by / and %.
    }
}
