using MCMirror.Internal;

namespace MinecraftTests {
    internal class MCTestTests {

        [MCTest(-230)]
        public static int ThisShouldPass() {
            return -230;
        }

        [MCTest(1000)]
        public static int ThisShouldFail() {
            return 230;
        }
    }
}
