using MCMirror.Internal;
using System;

namespace MinecraftTests {
    internal class FloatTests {

        [MCTest(1)]
        public static int TestAddition1() {
            if (2f + 2f == 4f)
                return 1;
            return 0;
        }

        [MCTest(1)]
        public static int TestAddition2() {
            if (1.75f + 0.3125f == 2.0625f)
                return 1;
            return 0;
        }
    }
}
