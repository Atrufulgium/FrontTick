using MCMirror;
using static MCMirror.Internal.RawMCFunction;

namespace MinecraftTests {
    internal class GotoTests {

        [MCFunction("fizzbuzz")]
        public static void FizzBuzz() {
            int counter, mod3, mod5, escapecheck;
            counter = 1;
        loopstart:
            mod3 = counter;
            mod3 %= 3;
            mod5 = counter;
            mod5 %= 5;
            if (mod3 != 0) {
                if (mod5 != 0) {
                    Run("tellraw @a {\"score\":{\"name\":\"#compiled:fizzbuzz#counter\",\"objective\":\"_\"}}");
                } else {
                    Run("tellraw @a {\"text\":\"Buzz\"}");
                }
            } else {
                if (mod5 != 0) {
                    Run("tellraw @a {\"text\":\"Fizz\"}");
                } else {
                    Run("tellraw @a {\"text\":\"FizzBuzz\"}");
                }
            }
            escapecheck = counter;
            escapecheck -= 100;
            counter += 1;
            if (escapecheck != 0) {
                goto loopstart;
            }
            Run("say Done!");
        }
    }
}
