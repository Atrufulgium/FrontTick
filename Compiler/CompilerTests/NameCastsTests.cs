using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameCastsTests {

        [TestMethod]
        public void CastTest1()
            => TestCompilationSucceeds(@"
using MCMirror;
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = (int)t;
    }

    public static explicit operator int(Test t) { return t.val; }
}
", @"
# (File compiled:internal/test.cast-explicit-int32-test.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.cast-explicit-int32-test##arg0#val _

# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #compiled:internal/test.cast-explicit-int32-test##arg0#val _ = #compiled:internal/test.testmethod##arg0#val _
function compiled:internal/test.cast-explicit-int32-test
scoreboard players operation #compiled:internal/test.testmethod##arg1 _ = #RET _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void CastTest2()
            => TestCompilationSucceeds(@"
using MCMirror;
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = t;
    }

    public static implicit operator int(Test t) { return t.val; }
}
", @"
# (File compiled:internal/test.cast-implicit-int32-test.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.cast-implicit-int32-test##arg0#val _

# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #compiled:internal/test.cast-implicit-int32-test##arg0#val _ = #compiled:internal/test.testmethod##arg0#val _
function compiled:internal/test.cast-implicit-int32-test
scoreboard players operation #compiled:internal/test.testmethod##arg1 _ = #RET _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}