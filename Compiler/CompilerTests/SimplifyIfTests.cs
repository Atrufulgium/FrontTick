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
        int IFTEMP0;
        IFTEMP0 = Sum(i,j);
        if (IFTEMP0 == 3) {
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