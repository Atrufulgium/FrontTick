using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class SimplifyIfTests {

        [TestMethod]
        public void IfTest1()
            => TestCompilationSucceeds(@"
internal struct Test {
    static void TestMethod(int i, int j) {
        if (Sum(i,j) == 3) {
            i = 4;
        }
    }
    static int Sum(int i, int j) {
        i += j;
        return i;
    }
}
", @"
# (File compiled:internal/test.sum.mcfunction)
scoreboard players operation #compiled:internal/test.sum##arg0 _ += #compiled:internal/test.sum##arg1 _
scoreboard players operation #RET _ = #compiled:internal/test.sum##arg0 _

# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #compiled:internal/test.sum##arg0 _ = #compiled:internal/test.testmethod##arg0 _
scoreboard players operation #compiled:internal/test.sum##arg1 _ = #compiled:internal/test.testmethod##arg1 _
function compiled:internal/test.sum
scoreboard players operation #compiled:internal/test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:internal/test.testmethod##IFTEMP0 _ matches 3 run scoreboard players set #compiled:internal/test.testmethod##arg0 _ 4
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}