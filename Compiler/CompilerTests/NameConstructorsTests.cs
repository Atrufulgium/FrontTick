using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameConstructorsTests {

        // This is Raw because we can't pull a #RET in the to-be-compared code.
        // Also note that this has some useless "set #RET#val 0" commands. This
        // is *currently* correct because I don't check for reads/writes yet.
        // Ouch it's inefficient lol
        [TestMethod]
        public void NameConstructorTest1Raw()
            => TestCompilationSucceedsRaw(@"
internal struct Test {
    int val;

    public Test(int value) {
        val = value;
    }

    static Test TestMethod() {
        return new Test(230);
    }
}
", @"
# (File (functions) compiled:internal/test.-construct-.mcfunction)
scoreboard players set #RET#val _ 0
scoreboard players set #RET#val _ 0

# (File (functions) compiled:internal/test.-construct--int32.mcfunction)
scoreboard players set #RET#val _ 0
scoreboard players set #RET#val _ 0
scoreboard players operation #RET#val _ = #compiled:internal/test.-construct--int32##arg0 _

# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.-construct--int32##arg0 _ 230
function compiled:internal/test.-construct--int32
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Here it's an order thing I can't emulate in non-raw code; putting
        // the =3 in the constructor results in "=default" and then "=3". Also
        // fixed once I do read/write-analysis.
        [TestMethod]
        public void NameConstructorTest2Raw()
            => TestCompilationSucceedsRaw(@"
internal struct Test {
    int val = 3;

    public Test() {}
}
", @"
# (File (functions) compiled:internal/test.-construct-.mcfunction)
scoreboard players set #RET#val _ 0
scoreboard players set #RET#val _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Adding a c# test
        //   [MCMirror.TrueLoad] static void CONSTRUCTSTATIC()
        // makes the namemanager make (justified) complaints about a double
        // registration, as there's also a static constructor setting the value
        // to the default. So raw it is...
        // Also again the double assignment because of no read/write analysis.
        [TestMethod]
        public void NameStaticConstructorTest1Raw()
            => TestCompilationSucceedsRaw(@"
internal struct Test {
    static int val;

    static Test() {
        val = 3;
    }
}
", @"
# (File (functions) compiled:internal/test.-constructstatic-.mcfunction)
scoreboard players set #compiled:test#val _ 0
scoreboard players set #compiled:test#val _ 3

# Method Attributes:
#   [MCMirror.TrueLoad]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameStaticConstructorTest2Raw()
            => TestCompilationSucceedsRaw(@"
internal struct Test {
    static int val = 3;
}
", @"
# (File (functions) compiled:internal/test.-constructstatic-.mcfunction)
scoreboard players set #compiled:test#val _ 3

# Method Attributes:
#   [MCMirror.TrueLoad]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameStaticConstructorTest3Raw()
            => TestCompilationSucceedsRaw(@"
internal struct Test {
    static int val1 = 10;
    static int val2;

    static Test() {
        val2 = 20;
    }
}
", @"
# (File (functions) compiled:internal/test.-constructstatic-.mcfunction)
scoreboard players set #compiled:test#val1 _ 10
scoreboard players set #compiled:test#val2 _ 0
scoreboard players set #compiled:test#val2 _ 20

# Method Attributes:
#   [MCMirror.TrueLoad]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Requires properly handled initialization.
//        [TestMethod]
//        public void NameStaticAndNormalConstructorTest1()
//            => TestCompilationSucceedsTheSame(@"
//internal struct Test {
//    static int val1 = 10;
//    static int val2;
//    int val3 = 30;
//    int val4;

//    static Test() {
//        val2 = 20;
//    }

//    Test(int i) {
//        val4 = i;
//    }
//}
//", @"
//internal struct Test {
//    static int val1;
//    static int val2;
//    int val3;
//    int val4;

//    static Test() {
//        val1 = 10;
//        val2 = 20;
//    }

//    Test(int i) {
//        val3 = 30;
//        val4 = i;
//    }

//    public Test() {
//        val3 = 30;
//    }
//}
//", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}