using MCMirror.Internal;

namespace MinecraftTests {
    internal class UIntTests {

        [MCTest(26098562)]
        public static int TestDivision1() {
            uint a = 3210123210u;
            uint b = 123u;
            return (int)(a / b);
        }

        [MCTest(1)]
        public static int TestDivision2() {
            uint a = 3210123210u;
            uint b = 2345678910u;
            return (int)(a / b);
        }

        [MCTest(0)]
        public static int TestDivision3() {
            uint a = 123u;
            uint b = 3210123210u;
            return (int)(a / b);
        }

        [MCTest(84)]
        public static int TestMod1() {
            uint a = 3210123210u;
            uint b = 123u;
            return (int)(a % b);
        }

        [MCTest(864444300)]
        public static int TestMod2() {
            uint a = 3210123210u;
            uint b = 2345678910u;
            return (int)(a % b);
        }

        [MCTest(123)]
        public static int TestMod3() {
            uint a = 123u;
            uint b = 3210123210u;
            return (int)(a % b);
        }
    }
}
