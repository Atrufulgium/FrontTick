using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class LoopsToGotoTests {

        [TestMethod]
        public void WhileTest1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
        }
    }
}
");

        [TestMethod]
        public void WhileTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loopend;
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            break;
        }
    }
}
");

        [TestMethod]
        public void WhileTest3()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 1)
                goto loopend;
            goto loop;
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 1)
                break;
        }
    }
}
");

        [TestMethod]
        public void WhileTest4()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop1:
        if (i == 0) {
            i = 1;
        loop2:
            if (i == 2) {
                i = 3;
                if (i == 4)
                    goto loop2break;
            loop3:
                if (i == 5) {
                    i = 6;
                    goto loop3;
                }
                goto loop2;
            }
        loop2break:
            goto loop1;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            while (i == 2) {
                i = 3;
                if (i == 4)
                    break;
                while (i == 5) {
                    i = 6;
                }
            }
        }
    }
}
");

        [TestMethod]
        public void WhileTest5()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            continue;
        }
    }
}
");

        [TestMethod]
        public void WhileTest6()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 2)
                goto loop;
            i = 3;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 2)
                continue;
            i = 3;
        }
    }
}
");

        // TODO: This testcase shows an optimisation opportunity.
        [TestMethod]
        public void WhileTest7()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 2)
                goto loop;
            else
                goto loopend;
            goto loop; // This is unfortunate!
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 2)
                continue;
            else
                break;
        }
    }
}
");
    }
}