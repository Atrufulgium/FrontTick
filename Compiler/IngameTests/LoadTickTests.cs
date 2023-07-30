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
    }
}
