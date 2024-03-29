﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        Struct ⵌcalltemp0;
        Struct ⵌcalltemp1;
        ⵌcalltemp0 = a + b;
        ⵌcalltemp1 = ⵌcalltemp0 + b;
        c = ⵌcalltemp1 + a;
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
        int ⵌcalltemp0;
        int ⵌcalltemp1;
        ⵌcalltemp0 = Identity(i);
        ⵌcalltemp1 = Identity(ⵌcalltemp0);
        return Identity(ⵌcalltemp1);
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
            int ⵌcalltemp0;
            int ⵌcalltemp1;
            ⵌcalltemp0 = Identity(i);
            ⵌcalltemp1 = Identity(ⵌcalltemp0);
            Identity(ⵌcalltemp1);
            if (i == 3)
                goto label;
    }

    public static int Identity(int i) { return i; }
}
");

        // BROKEN: The "goto breaklabel" is at the wrong place.
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
public class Test {
    public static void TestMethod(int i) {
        int ⵌcalltemp0, ⵌcalltemp1, ⵌcalltemp2;
        while(true) {
            ⵌcalltemp0 = Identity(i);
            ⵌcalltemp1 = Identity(ⵌcalltemp0);
            ⵌcalltemp2 = Identity(ⵌcalltemp1);
            if (ⵌcalltemp2 != 0)
                break;
            i = i;
        }
    }

    public static int Identity(int i) { return i; }
}
");
    }
}