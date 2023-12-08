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

        [TestMethod]
        public void PropertyTest2()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    static int val;
    public static int Val { get { return val; } private set { val = value; } }

    public static void TestMethod(int i) {
        Val += i;
    }
}
", @"
public class Test {
    static int val;
    public static int GETVal() { return val; }
    public static void SETVal(int value) { val = value; }

    public static void TestMethod(int i) {
        SETVal(GETVal() + i);
    }
}
");

        [TestMethod]
        public void PropertyTest3()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static void TestMethod(int i) {
        i = Other.Val;
        Other.Val = 4;
    }
}
public class Other {
    static int val;
    public static int Val { get { return val; } set { val = value; } }
}
", @"
public class Test {
    public static void TestMethod(int i) {
        i = Other.GETVal();
        Other.SETVal(4);
    }
}
public class Other {
    static int val;
    public static int GETVal() { return val; }
    public static void SETVal(int value) { val = value; }
}
");

        [TestMethod]
        public void AutoPropertyTest1()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int Val { get; set; }

    public static void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
", @"
public class Test {
    static int AUTOPROPERTYVal;
    public static int Val { get => AUTOPROPERTYVal; set => AUTOPROPERTYVal = value; }

    public static void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
");

        // Note CS1605: Classes may not have `ref this` in the constructor.
        // Fix later, once I actualy care about classes.
        [TestMethod]
        public void AutoPropertyTest2()
            => TestCompilationSucceedsTheSame(@"
public struct Test {
    public int Val { get; set; }

    public void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
", @"
public struct Test {
    int AUTOPROPERTYVal;
    public int Val { get => AUTOPROPERTYVal; set => AUTOPROPERTYVal = value; }

    public void TestMethod(int i) {
        i = Val;
        Val = 4;
    }
}
");

        [TestMethod]
        public void InitPropertyTest1()
            => TestCompilationSucceedsTheSame(@"
public struct Test {
    public int Val { get; init; }

    public Test(int i) {
        i = Val;
        Val = 4;
    }
}
", @"
public struct Test {
    public int Val { get; set; }

    public Test(int i) {
        i = Val;
        Val = 4;
    }
}
");
    }
}