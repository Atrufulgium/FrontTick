using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class FlattenNestedCallsTests {

        // Pretty inefficient generated code, but that's an issue for later.
        [TestMethod]
        public void FlattenTest1()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    public static void TestMethod(Struct a, Struct b) {
        Struct c;
        c = a + b + b + a;
    }
}

public struct Struct {
    public int val;
    public static Struct operator +(Struct a, Struct b) {
        a.val += b.val;
        return a;
    }
}
", @"
# (File compiled:internal/struct.operator-add.mcfunction)
scoreboard players operation #compiled:internal/struct.operator-add##arg0#val _ += #compiled:internal/struct.operator-add##arg1#val _
scoreboard players operation #RET#val _ = #compiled:internal/struct.operator-add##arg0#val _

# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #compiled:internal/struct.operator-add##arg0#val _ = #compiled:internal/test.testmethod##arg0#val _
scoreboard players operation #compiled:internal/struct.operator-add##arg1#val _ = #compiled:internal/test.testmethod##arg1#val _
function compiled:internal/struct.operator-add
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP0#val _ = #RET#val _
scoreboard players operation #compiled:internal/struct.operator-add##arg0#val _ = #compiled:internal/test.testmethod##CALLTEMP0#val _
scoreboard players operation #compiled:internal/struct.operator-add##arg1#val _ = #compiled:internal/test.testmethod##arg1#val _
function compiled:internal/struct.operator-add
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP1#val _ = #RET#val _
scoreboard players operation #compiled:internal/struct.operator-add##arg0#val _ = #compiled:internal/test.testmethod##CALLTEMP1#val _
scoreboard players operation #compiled:internal/struct.operator-add##arg1#val _ = #compiled:internal/test.testmethod##arg0#val _
function compiled:internal/struct.operator-add
scoreboard players operation #compiled:internal/test.testmethod#c#val _ = #RET#val _
");

        [TestMethod]
        public void FlattenTest2()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    public static int TestMethod(int i) {
        return Identity(Identity(Identity(i)));
    }

    public static int Identity(int i) { return i; }
}
", @"
# (File compiled:internal/test.identity.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.identity##arg0 _

# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##arg0 _
function compiled:internal/test.identity
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP0 _ = #RET _
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##CALLTEMP0 _
function compiled:internal/test.identity
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP1 _ = #RET _
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##CALLTEMP1 _
function compiled:internal/test.identity
");

        [TestMethod]
        public void FlattenTest3()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        label: Identity(Identity(Identity(i)));
        if (i == 3)
            goto label;
    }

    public static int Identity(int i) { return i; }
}
", @"
# (File compiled:internal/test.identity.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.identity##arg0 _

# (File compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.testmethod-0-goto-label-1

# (File compiled:internal/test.testmethod-0-goto-label-1.mcfunction)
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##arg0 _
function compiled:internal/test.identity
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP0 _ = #RET _
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##CALLTEMP0 _
function compiled:internal/test.identity
scoreboard players operation #compiled:internal/test.testmethod##CALLTEMP1 _ = #RET _
scoreboard players operation #compiled:internal/test.identity##arg0 _ = #compiled:internal/test.testmethod##CALLTEMP1 _
function compiled:internal/test.identity
execute if score #compiled:internal/test.testmethod##arg0 _ matches 3 run scoreboard players set #GOTOFLAG _ 1
execute if score #GOTOFLAG _ matches 1 run function compiled:internal/test.testmethod-3-if-branch

# (File compiled:internal/test.testmethod-3-if-branch.mcfunction)
scoreboard players set #GOTOFLAG _ 0
function compiled:internal/test.testmethod-0-goto-label-1
");

        [TestMethod]
        public void FlattenTest4()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while(Identity(Identity(Identity(i))) != 0) {
            i = i;
        }
    }

    public static int Identity(int i) { return i; }
}
", @"
(this fails because general condition extraction isn't done yet)
");
    }
}