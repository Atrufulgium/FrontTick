using MCMirror;
using static MCMirror.Internal.RawMCFunction;

namespace MinecraftTests {
    /// <summary>
    /// This is not really testable in the traditional sense. So to see whether
    /// stuff works, do
    /// <code>
    ///     /scoreboard players set test_load_tick _ 1
    /// </code>
    /// and to turn it off, do
    /// <code>
    ///     /scoreboard players set test_load_tick _ 0
    /// </code>
    /// </summary>
    internal class LoadTickTests {

        [Load]
        public static void LoadMethod() {
            Run("execute if score test_load_tick _ matches 1 run say load");
        }

        [Tick]
        public static void TickMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick");
        }

        [TrueLoad]
        public static void TrueLoadMethod() {
            Run("execute if score test_load_tick _ matches 1 run say trueload");
        }

        [Tick(2)]
        public static void Tick2Method() {
            Run("execute if score test_load_tick _ matches 1 run say tick 2");
        }

        [Tick(4)]
        public static void Tick4aMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 4a");
        }

        [Tick(4)]
        public static void Tick4bMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 4b");
        }

        [Tick(5)]
        public static void Tick5aMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5a");
        }

        [Tick(5)]
        public static void Tick5bMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5b");
        }

        [Tick(5)]
        public static void Tick5cMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5c");
        }

        [Tick(5)]
        public static void Tick5dMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5d");
        }

        [Tick(5)]
        public static void Tick5eMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5e");
        }

        [Tick(5)]
        public static void Tick5fMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5f");
        }

        [Tick(5)]
        public static void Tick5gMethod() {
            Run("execute if score test_load_tick _ matches 1 run say tick 5g");
        }
    }
}
