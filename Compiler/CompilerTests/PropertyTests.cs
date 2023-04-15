using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class PropertyTests {

        [TestMethod]
        public void PropertyTest1()
            => TestCompilationSucceeds(@"
public class Test {
    static int val;
    public static int Val { get { return val; } private set { val = value; } }

    public static void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
", @"
# (File compiled:internal/test.get-val.mcfunction)
scoreboard players operation #RET _ = #compiled:test#val _

# (File compiled:internal/test.set-val.mcfunction)
scoreboard players operation #compiled:test#val _ = #compiled:internal/test.set-val##arg0 _

# (File compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.get-val
scoreboard players operation #compiled:internal/test.testmethod##arg0 _ = #RET _
scoreboard players set #compiled:internal/test.set-val##arg0 _ 4
function compiled:internal/test.set-val
");

        // MCInt because not yet rewriting `a+b` into `a += b` for ints.
        // Might not do that ever, because it will be mcint'ed anyway.
        // Awkward? Yes. Does the test output seem correct? Also yes.
        [TestMethod]
        public void PropertyTest2()
            => TestCompilationSucceeds(@"
using MCMirror.Internal.Primitives;
public class Test {
    static MCInt val;
    public static MCInt Val { get { return val; } private set { val = value; } }

    public static void TestMethod(MCInt i) {
        Val += i;
    }
}
", @"
# (File compiled:internal/test.get-val.mcfunction)
scoreboard players operation #RET#val _ = #compiled:test#val#val _

# (File compiled:internal/test.set-val.mcfunction)
scoreboard players operation #compiled:test#val#val _ = #compiled:internal/test.set-val##arg0#val _

# (File compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.get-val
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP0#val _ = #RET#val _
scoreboard players operation #compiled:internal/mcmirror.internal.primitives.mcint.operator-add##arg0#val _ = #compiled:internal/test.testmethod##CALLTEMP0#val _
scoreboard players operation #compiled:internal/mcmirror.internal.primitives.mcint.operator-add##arg1#val _ = #compiled:internal/test.testmethod##arg0#val _
function compiled:internal/mcmirror.internal.primitives.mcint.operator-add
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP1#val _ = #RET#val _
scoreboard players operation #compiled:internal/test.set-val##arg0#val _ = #compiled:internal/test.testmethod##CALLTEMP1#val _
function compiled:internal/test.set-val
");
    }
}