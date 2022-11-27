using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameOperatorsTests {

        [TestMethod]
        public void OperatorTest1()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    public static void TestMethod(Struct a, Struct b) {
        Struct c;
        c = a + b;
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
scoreboard players operation #compiled:internal/test.testmethod#c#val _ = #RET#val _
");
    }
}