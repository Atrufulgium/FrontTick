using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class SplitDeclarationsTests {

        [TestMethod]
        public void SplitTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static void TestMethod() {
        int i = 3;
    }
}
", @"
internal struct Test {
    static void TestMethod() {
        int i;
        i = 3;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void SplitTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static void TestMethod() {
        int i = 3, j = 4, k = 5;
    }
}
", @"
internal struct Test {
    static void TestMethod() {
        int i, j, k;
        i = 3;
        j = 4;
        k = 5;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });
    }
}