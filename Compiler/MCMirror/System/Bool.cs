using static MCMirror.Internal.CompileTime;
using static MCMirror.Internal.RawMCFunction;

namespace System {
    // The internal representation of bools in MCFunction are defined to be
    // 0 -- false       1 -- true.
    // Other values are invalid and assumed not to happen.
    public struct Boolean {
        // TODO: Short circuiting &&, ||.

        public static bool operator !(bool value) {
            // res = 1 - value
            bool res;
            res = true;
            Run($"scoreboard players operation {VarName(res)} _ -= {VarName(value)} _");
            return res;
        }

        public static bool operator &(bool a, bool b) {
            // res = a * b
            bool res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ *= {VarName(b)} _");
            return res;
        }

        public static bool operator |(bool a, bool b) {
            // res = 1
            // if (both zero) res = 0
            bool res;
            res = true;
            Run($"execute if score {VarName(a)} _ matches 0 if score {VarName(b)} _ matches 0 run scoreboard players set {VarName(res)} _ 0");
            return res;
        }

        public static bool operator ^(bool a, bool b) {
            // res = a + b
            // if (res == 2) res = 0
            bool res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ += {VarName(b)} _");
            Run($"execute if score {VarName(res)} _ matches 2 run scoreboard players set {VarName(res)} _ 0");
            return res;
        }

        public static bool operator ==(bool a, bool b) {
            // Directly mcfunction's comparison
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator !=(bool a, bool b) {
            // Directly mcfunction's comparison
            bool res;
            res = false;
            Run($"execute unless score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }
    }
}
