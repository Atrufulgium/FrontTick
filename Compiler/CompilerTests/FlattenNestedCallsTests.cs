using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class FlattenNestedCallsTests {

        // Pretty inefficient generated code, but that's an issue for later.
        [TestMethod]
        public void FlattenTest1()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static void TestMethod(Struct a, Struct b) {
        Struct c;
        c = a + b + b + a;
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
        Struct CALLTEMP0;
        Struct CALLTEMP1;
        CALLTEMP0 = a + b;
        CALLTEMP1 = CALLTEMP0 + b;
        c = CALLTEMP1 + a;
    }
}

public struct Struct {
    public int val;
    public static Struct operator +(Struct a, Struct b) {
        a.val += b.val;
        return a;
    }
}
");

        [TestMethod]
        public void FlattenTest2()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int TestMethod(int i) {
        return Identity(Identity(Identity(i)));
    }

    public static int Identity(int i) { return i; }
}
", @"
public class Test {
    public static int TestMethod(int i) {
        int CALLTEMP0;
        int CALLTEMP1;
        CALLTEMP0 = Identity(i);
        CALLTEMP1 = Identity(CALLTEMP0);
        return Identity(CALLTEMP1);
    }

    public static int Identity(int i) { return i; }
}
");

        [TestMethod]
        public void FlattenTest3()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static void TestMethod(int i) {
        label: Identity(Identity(Identity(i)));
        if (i == 3)
            goto label;
    }

    public static int Identity(int i) { return i; }
}
", @"
public class Test {
    public static void TestMethod(int i) {
        label:
            int CALLTEMP0;
            int CALLTEMP1;
            CALLTEMP0 = Identity(i);
            CALLTEMP1 = Identity(CALLTEMP0);
            Identity(CALLTEMP1);
            if (i == 3)
                goto label;
    }

    public static int Identity(int i) { return i; }
}
");

        [TestMethod]
        public void FlattenTest4()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static void TestMethod(int i) {
        while(Identity(Identity(Identity(i))) != 0) {
            i = i;
        }
    }

    public static int Identity(int i) { return i; }
}
", @"
// (this fails because general condition extraction isn't done yet)
");
    }
}