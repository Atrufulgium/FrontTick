using MCMirror.Internal;

namespace MinecraftTests {
    internal class UIntTests {

        [MCTest(26098562u)]
        public static uint TestDivision1() {
            uint a = 3210123210u;
            uint b = 123u;
            return a / b;
        }

        [MCTest(1u)]
        public static uint TestDivision2() {
            uint a = 3210123210u;
            uint b = 2345678910u;
            return a / b;
        }

        [MCTest(0u)]
        public static uint TestDivision3() {
            uint a = 123u;
            uint b = 3210123210u;
            return a / b;
        }

        [MCTest(84u)]
        public static uint TestMod1() {
            uint a = 3210123210u;
            uint b = 123u;
            return a % b;
        }

        [MCTest(864444300u)]
        public static uint TestMod2() {
            uint a = 3210123210u;
            uint b = 2345678910u;
            return a % b;
        }

        [MCTest(123u)]
        public static uint TestMod3() {
            uint a = 123u;
            uint b = 3210123210u;
            return a % b;
        }
    }
}
