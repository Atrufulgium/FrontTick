using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameCastsTests {

        [TestMethod]
        public void CastTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = (int)t;
    }

    public static explicit operator int(Test t) { return t.val; }
}
", @"
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = CASTEXPLICITInt32Test(t);
    }

    public static int CASTEXPLICITInt32Test(Test t) { return t.val; }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void CastTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = t;
    }

    public static implicit operator int(Test t) { return t.val; }
}
", @"
internal struct Test {
    int val;

    static void TestMethod(Test t, int i) {
        i = CASTIMPLICITInt32Test(t);
    }

    public static int CASTIMPLICITInt32Test(Test t) { return t.val; }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}