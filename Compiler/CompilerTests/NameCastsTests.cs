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
        i = CASTーEXPLICITーInt32ーTest(t);
    }

    public static int CASTーEXPLICITーInt32ーTest(Test t) { return t.val; }
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
        i = CASTーIMPLICITーInt32ーTest(t);
    }

    public static int CASTーIMPLICITーInt32ーTest(Test t) { return t.val; }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}