using static MCMirror.Internal.CompileTime;
using static MCMirror.Internal.RawMCFunction;

namespace System {
    public struct Int32 {

        public static int operator +(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ += {VarName(b)} _");
            return res;
        }

        public static int operator -(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ -= {VarName(b)} _");
            return res;
        }

        public static int operator *(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ *= {VarName(b)} _");
            return res;
        }

        public static int operator /(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ /= {VarName(b)} _");
            return res;
        }

        public static int operator %(int a, int b) {
            int res;
            res = a;
            Run($"scoreboard players operation {VarName(res)} _ %= {VarName(b)} _");
            return res;
        }

        public static bool operator ==(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator !=(int a, int b) {
            bool res;
            res = false;
            Run($"execute unless score {VarName(a)} _ = {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        // TODO: Constant branch using "matches" for these instead of "OP XXX _".
        public static bool operator <=(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ <= {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator <(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ < {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator >=(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ >= {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }

        public static bool operator >(int a, int b) {
            bool res;
            res = false;
            Run($"execute if score {VarName(a)} _ > {VarName(b)} _ run scoreboard players set {VarName(res)} _ 1");
            return res;
        }
    }
}
