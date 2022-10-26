using MCMirror;

namespace MinecraftTests {
    // These are written in the very specific subset of c# that
    // `ProcessedToDatapackWalker` can handle.
    internal class GotoTests {

        [MCTest(1)]
        public static int GotoTest1() {
            int i;
            i = 1;
            if (i != 0) {
                goto label;
            }
            i = 2;
        label:
            return i;
        }

        [MCTest(2)]
        public static int GotoTest2() {
            int i;
            i = 1;
            if (i == 0) {
                goto label;
            }
            i = 2;
        label:
            return i;
        }

        [MCTest(3)]
        public static int GotoTest3() {
            int i;
            i = 1;
            if (i != 0) {
                if (i != 0) {
                    i = 0;
                    if (i != 0) {
                        goto label1;
                    }
                    i = 3;
                    goto label2;
                }
                goto label2;
            }
        label1:
            i = 2;
        label2:
            return i;
        }

        [MCTest(6000)]
        public static int GotoTest4() {
            int x, y, z, counter;
            x = 0;
            counter = 0;
        xloop:
            y = 0;
        yloop:
            z = 0;
        zloop:
            counter += 1;
            z += 1;
            if (z != 10) {
                goto zloop;
            }
            y += 1;
            if (y != 20) {
                goto yloop;
            }
            x += 1;
            if (x != 30) {
                goto xloop;
            }
            return counter;
        }

        [MCTest(9)]
        public static int GotoTest5() {
            int i, j, gotocounter;
            i = 0;
            j = 0;
            gotocounter = 0;
            // This is a bit of a maze, purposefully.
            // Bit of a "this is so ridiculous never-to-be-encountered that if
            // this works, it definitely works". I know, not how you're
            // supposed to do testing, but most complexity of goto lives in
            // scoping and the relation with *other* goto statements, and I
            // don't feel like enumerating all interesting cases.
            // (Just run it in linqpad or whatever to see what should roll out)
        label1:
            i += 1;
        label2:
            if (i == 2) {
            label3:
                if (i == 2) {
                    i += 1;
                    gotocounter += 1;
                    goto label3;
                } else {
                    gotocounter += 1;
                    goto label1;
                }
            } else {
                if (i == 4) {
                    if (j == 1) {
                        gotocounter += 1;
                        goto label4;
                    }
                } else {
                    gotocounter += 1;
                    goto label1;
                }
            }
            i = 0;
            j = 1;
            gotocounter += 1;
            goto label2;
        label4:
            if (j == 0) {
                gotocounter += 1;
                goto label2;
            }
            return gotocounter;
        }

        // The "goto < label < goto" case in scoping is weird.
        // Definitely needs testing.
        [MCTest(27)]
        public static int GotoTest6() {
            int i;
            int j;
            i = 0;
            j = 1;
            if (i == 0)
                goto label;
            i += 1;
            j *= 2;
        label:
            i += 1;
            j *= 3;
            if (i != 3) {
                goto label;
            }
            return j;
        }
    }
}
