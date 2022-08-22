using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class GuaranteeBlockTests {

        [TestMethod]
        public void IfElseTest1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0) {
            i = 1;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0)
            i = 1;
    }
}
");
        [TestMethod]
        public void IfElseTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0) {
            i = 1;
        } else {
            i = 2;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0)
            i = 1;
        else {
            i = 2;
        }
    }
}
");
        [TestMethod]
        public void IfElseTest3()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0) {
            i = 1;
        } else {
            i = 2;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0) {
            i = 1;
        } else
            i = 2;
    }
}
");

        [TestMethod]
        public void IfElseTest4()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0) {
            i = 1;
        } else {
            i = 2;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        if (i == 0)
            i = 1;
        else
            i = 2;
    }
}
");
    }
}