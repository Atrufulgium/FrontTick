using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class PropertyTests {

        [TestMethod]
        public void PropertyTest1()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    static int val;
    public static int Val { get { return val; } private set { val = value; } }

    public static void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
", @"
public class Test {
    static int val;
    public static int GETVal() { return val; }
    public static void SETVal(int value) { val = value; }

    public static void TestMethod(int i) {
        i = GETVal();
        SETVal(4);
    }
}
");

        // MCInt because not yet rewriting `a+b` into `a += b` for ints.
        // Might not do that ever, because it will be mcint'ed anyway.
        [TestMethod]
        public void PropertyTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror.Internal.Primitives;
public class Test {
    static MCInt val;
    public static MCInt Val { get { return val; } private set { val = value; } }

    public static void TestMethod(MCInt i) {
        Val += i;
    }
}
", @"
using MCMirror.Internal.Primitives;
public class Test {
    static MCInt val;
    public static MCInt GETVal() { return val; }
    public static void SETVal(MCInt value) { val = value; }

    public static void TestMethod(MCInt i) {
        SETVal(GETVal() + i);
    }
}
");
    }
}