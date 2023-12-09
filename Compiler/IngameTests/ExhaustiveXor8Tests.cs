using MCMirror.Internal;
using System;

namespace MinecraftTests {
    public class ExhaustiveXor8Tests {

        [MCTest(0)]
        public static int Test0Xor0() {
            return BitHelpers.Xor8(0, 0);
        }

        [MCTest(1)]
        public static int Test0Xor1() {
            return BitHelpers.Xor8(0, 1);
        }

        [MCTest(2)]
        public static int Test0Xor2() {
            return BitHelpers.Xor8(0, 2);
        }

        [MCTest(3)]
        public static int Test0Xor3() {
            return BitHelpers.Xor8(0, 3);
        }

        [MCTest(4)]
        public static int Test0Xor4() {
            return BitHelpers.Xor8(0, 4);
        }

        [MCTest(5)]
        public static int Test0Xor5() {
            return BitHelpers.Xor8(0, 5);
        }

        [MCTest(6)]
        public static int Test0Xor6() {
            return BitHelpers.Xor8(0, 6);
        }

        [MCTest(7)]
        public static int Test0Xor7() {
            return BitHelpers.Xor8(0, 7);
        }

        [MCTest(1)]
        public static int Test1Xor0() {
            return BitHelpers.Xor8(1, 0);
        }

        [MCTest(0)]
        public static int Test1Xor1() {
            return BitHelpers.Xor8(1, 1);
        }

        [MCTest(3)]
        public static int Test1Xor2() {
            return BitHelpers.Xor8(1, 2);
        }

        [MCTest(2)]
        public static int Test1Xor3() {
            return BitHelpers.Xor8(1, 3);
        }

        [MCTest(5)]
        public static int Test1Xor4() {
            return BitHelpers.Xor8(1, 4);
        }

        [MCTest(4)]
        public static int Test1Xor5() {
            return BitHelpers.Xor8(1, 5);
        }

        [MCTest(7)]
        public static int Test1Xor6() {
            return BitHelpers.Xor8(1, 6);
        }

        [MCTest(6)]
        public static int Test1Xor7() {
            return BitHelpers.Xor8(1, 7);
        }

        [MCTest(2)]
        public static int Test2Xor0() {
            return BitHelpers.Xor8(2, 0);
        }

        [MCTest(3)]
        public static int Test2Xor1() {
            return BitHelpers.Xor8(2, 1);
        }

        [MCTest(0)]
        public static int Test2Xor2() {
            return BitHelpers.Xor8(2, 2);
        }

        [MCTest(1)]
        public static int Test2Xor3() {
            return BitHelpers.Xor8(2, 3);
        }

        [MCTest(6)]
        public static int Test2Xor4() {
            return BitHelpers.Xor8(2, 4);
        }

        [MCTest(7)]
        public static int Test2Xor5() {
            return BitHelpers.Xor8(2, 5);
        }

        [MCTest(4)]
        public static int Test2Xor6() {
            return BitHelpers.Xor8(2, 6);
        }

        [MCTest(5)]
        public static int Test2Xor7() {
            return BitHelpers.Xor8(2, 7);
        }

        [MCTest(3)]
        public static int Test3Xor0() {
            return BitHelpers.Xor8(3, 0);
        }

        [MCTest(2)]
        public static int Test3Xor1() {
            return BitHelpers.Xor8(3, 1);
        }

        [MCTest(1)]
        public static int Test3Xor2() {
            return BitHelpers.Xor8(3, 2);
        }

        [MCTest(0)]
        public static int Test3Xor3() {
            return BitHelpers.Xor8(3, 3);
        }

        [MCTest(7)]
        public static int Test3Xor4() {
            return BitHelpers.Xor8(3, 4);
        }

        [MCTest(6)]
        public static int Test3Xor5() {
            return BitHelpers.Xor8(3, 5);
        }

        [MCTest(5)]
        public static int Test3Xor6() {
            return BitHelpers.Xor8(3, 6);
        }

        [MCTest(4)]
        public static int Test3Xor7() {
            return BitHelpers.Xor8(3, 7);
        }

        [MCTest(4)]
        public static int Test4Xor0() {
            return BitHelpers.Xor8(4, 0);
        }

        [MCTest(5)]
        public static int Test4Xor1() {
            return BitHelpers.Xor8(4, 1);
        }

        [MCTest(6)]
        public static int Test4Xor2() {
            return BitHelpers.Xor8(4, 2);
        }

        [MCTest(7)]
        public static int Test4Xor3() {
            return BitHelpers.Xor8(4, 3);
        }

        [MCTest(0)]
        public static int Test4Xor4() {
            return BitHelpers.Xor8(4, 4);
        }

        [MCTest(1)]
        public static int Test4Xor5() {
            return BitHelpers.Xor8(4, 5);
        }

        [MCTest(2)]
        public static int Test4Xor6() {
            return BitHelpers.Xor8(4, 6);
        }

        [MCTest(3)]
        public static int Test4Xor7() {
            return BitHelpers.Xor8(4, 7);
        }

        [MCTest(5)]
        public static int Test5Xor0() {
            return BitHelpers.Xor8(5, 0);
        }

        [MCTest(4)]
        public static int Test5Xor1() {
            return BitHelpers.Xor8(5, 1);
        }

        [MCTest(7)]
        public static int Test5Xor2() {
            return BitHelpers.Xor8(5, 2);
        }

        [MCTest(6)]
        public static int Test5Xor3() {
            return BitHelpers.Xor8(5, 3);
        }

        [MCTest(1)]
        public static int Test5Xor4() {
            return BitHelpers.Xor8(5, 4);
        }

        [MCTest(0)]
        public static int Test5Xor5() {
            return BitHelpers.Xor8(5, 5);
        }

        [MCTest(3)]
        public static int Test5Xor6() {
            return BitHelpers.Xor8(5, 6);
        }

        [MCTest(2)]
        public static int Test5Xor7() {
            return BitHelpers.Xor8(5, 7);
        }

        [MCTest(6)]
        public static int Test6Xor0() {
            return BitHelpers.Xor8(6, 0);
        }

        [MCTest(7)]
        public static int Test6Xor1() {
            return BitHelpers.Xor8(6, 1);
        }

        [MCTest(4)]
        public static int Test6Xor2() {
            return BitHelpers.Xor8(6, 2);
        }

        [MCTest(5)]
        public static int Test6Xor3() {
            return BitHelpers.Xor8(6, 3);
        }

        [MCTest(2)]
        public static int Test6Xor4() {
            return BitHelpers.Xor8(6, 4);
        }

        [MCTest(3)]
        public static int Test6Xor5() {
            return BitHelpers.Xor8(6, 5);
        }

        [MCTest(0)]
        public static int Test6Xor6() {
            return BitHelpers.Xor8(6, 6);
        }

        [MCTest(1)]
        public static int Test6Xor7() {
            return BitHelpers.Xor8(6, 7);
        }

        [MCTest(7)]
        public static int Test7Xor0() {
            return BitHelpers.Xor8(7, 0);
        }

        [MCTest(6)]
        public static int Test7Xor1() {
            return BitHelpers.Xor8(7, 1);
        }

        [MCTest(5)]
        public static int Test7Xor2() {
            return BitHelpers.Xor8(7, 2);
        }

        [MCTest(4)]
        public static int Test7Xor3() {
            return BitHelpers.Xor8(7, 3);
        }

        [MCTest(3)]
        public static int Test7Xor4() {
            return BitHelpers.Xor8(7, 4);
        }

        [MCTest(2)]
        public static int Test7Xor5() {
            return BitHelpers.Xor8(7, 5);
        }

        [MCTest(1)]
        public static int Test7Xor6() {
            return BitHelpers.Xor8(7, 6);
        }

        [MCTest(0)]
        public static int Test7Xor7() {
            return BitHelpers.Xor8(7, 7);
        }
    }
}
