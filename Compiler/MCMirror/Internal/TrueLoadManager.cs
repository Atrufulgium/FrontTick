using static MCMirror.Internal.CompileTime;
using static MCMirror.Internal.RawMCFunction;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This class contains the code that ensures [TrueLoad] only runs once
    /// per version of the datapack.
    /// </para>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Visible to .mcfunction and the compiler.")]
    [CompilerUsesName]
    public static class TrueLoadManager {

        [CompilerUsesName]
        const string varname = "trueloaded";
        [CompilerUsesName]
        public const string TrueLoadTagname = "internal/--trueload--";

        [Load]
        static void TrueLoader() {
            int state;
            state = 0;
            // TODO: In order to support updates, instead of setting to "1",
            // set to a random compile-time determined int.
            Run($"execute unless score #{CurrentNamespace()}:{varname} _ matches {TrueLoadValue()} run scoreboard players set {VarName(state)} _ 1");
            if (state == 1) {
                Run($"scoreboard players set #{CurrentNamespace()}:{varname} _ {TrueLoadValue()}");
                Run($"function #{CurrentNamespace()}:{TrueLoadTagname}");
            }
        }

        /// <summary>
        /// A manual reset button for the "only once" guarantee.
        /// </summary>
        [MCFunction("internal/--reset-true-load-state--")]
        static void ResetTrueLoad() {
            Run($"scoreboard players reset #{CurrentNamespace()}:{varname} _");
        }
    }
}
