using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameOperatorsTests {

        [TestMethod]
        public void OperatorTest1()
            => TestCompilationSucceedsTheSame(@"
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
public class Test {
    public static void TestMethod(Struct a, Struct b) {
        Struct c;
        c = Struct.OPERATORーADD(a, b);
    }
}

public struct Struct {
    public int val;
    public static Struct OPERATORーADD(Struct a, Struct b) {
        a.val += b.val;
        return a;
    }
}");
    }
}