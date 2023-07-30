using MCMirror.Internal;

namespace MinecraftTests {
    internal class BranchTests {

        [MCTest(1)]
        // Found a bug where the conditional's value isn't stored for the else
        // branch, and can change in the meantime, resulting in *both* branches
        // being run.
        public static int TestBranch1() {
            int i, j;
            i = 0;
            j = 0;
            if (i == 0) {
                i = 1;
                j += 1;
            } else {
                j += 1;
            }
            return j;
        }
    }
}
