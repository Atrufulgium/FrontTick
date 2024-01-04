using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class SimplifyIfTests {

        [TestMethod]
        public void IfTest1()
            => TestCompilationSucceedsTheSame(@"
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
internal struct Test {
    static void TestMethod(int i, int j) {
        bool ⵌiftemp0;
        ⵌiftemp0 = Sum(i,j) == 3;
        if (ⵌiftemp0) {
            i = 4;
        }
    }
    static int Sum(int i, int j) {
        i += j;
        return i;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}