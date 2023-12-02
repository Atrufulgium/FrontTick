using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class ProcessedToDatapackTests {

        #region declaration tests
        // Raw to test the output format only.
        [TestMethod]
        public void DeclarationTest1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
    }
}
", "# (File (functions) compiled:test.testmethod.mcfunction)\n# (Empty)\n\n# Method Attributes:\n#   [MCFunction]",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void DeclarationTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i, j;
        int k;
    }
}
", @"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
    }
}
",              new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void DeclarationTestWrong1()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i = 3;
    }
}
", CompilationException.ToDatapackDeclarationsMayNotBeInitializers,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void DeclarationTestWrong2()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i, j, k = 3, l, m = 2;
    }
}
", CompilationException.ToDatapackDeclarationsMayNotBeInitializers,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region assignment tests
        // Raw to test ∘= const.
        [TestMethod]
        public void AssignmentTest1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 0;
        i += 1;
        i -= 2;
        i *= 3;
        i /= 4;
        i %= 5;
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-add-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-add-int32-int32##arg1 _ 1
function compiled:internal/int.operator-add-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-sub-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-sub-int32-int32##arg1 _ 2
function compiled:internal/int.operator-sub-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-mul-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-mul-int32-int32##arg1 _ 3
function compiled:internal/int.operator-mul-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-div-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-div-int32-int32##arg1 _ 4
function compiled:internal/int.operator-div-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-mod-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-mod-int32-int32##arg1 _ 5
function compiled:internal/int.operator-mod-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        // Raw to test ∘= var.
        public void AssignmentTest2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i, j;
        j = 0;
        i = j;
        i += j;
        i -= j;
        i *= j;
        i /= j;
        i %= j;
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players operation #compiled:test.testmethod#i _ = #compiled:test.testmethod#j _
scoreboard players operation #compiled:internal/int.operator-add-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-add-int32-int32##arg1 _ = #compiled:test.testmethod#j _
function compiled:internal/int.operator-add-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-sub-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-sub-int32-int32##arg1 _ = #compiled:test.testmethod#j _
function compiled:internal/int.operator-sub-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-mul-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-mul-int32-int32##arg1 _ = #compiled:test.testmethod#j _
function compiled:internal/int.operator-mul-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-div-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-div-int32-int32##arg1 _ = #compiled:test.testmethod#j _
function compiled:internal/int.operator-div-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _
scoreboard players operation #compiled:internal/int.operator-mod-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-mod-int32-int32##arg1 _ = #compiled:test.testmethod#j _
function compiled:internal/int.operator-mod-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // (This test also depends on "return" working properly.)
        // Raw to test ∘= call.
        [TestMethod]
        public void AssignmentTest3Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 3;
        i -= GetThree();
    }
    public static int GetThree() { return 3; }
}
", @"
# (File (functions) compiled:internal/test.getthree.mcfunction)
scoreboard players set #RET _ 3

# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 3
function compiled:internal/test.getthree
scoreboard players operation #compiled:test.testmethod##CALLTEMP0 _ = #RET _
scoreboard players operation #compiled:internal/int.operator-sub-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players operation #compiled:internal/int.operator-sub-int32-int32##arg1 _ = #compiled:test.testmethod##CALLTEMP0 _
function compiled:internal/int.operator-sub-int32-int32
scoreboard players operation #compiled:test.testmethod#i _ = #RET _

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // (There's no method overloading yet and my own int has no impl yet, so broken.)
        [TestMethod]
        public void AssignmentTest4()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 3;
        i += - + + - + - 3;
    }
}
", @"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 3;
        i += -3;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTestWrong4()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = ~3;
    }
}
", CompilationException.ToDatapackUnsupportedUnary,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTestWrong5()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        float i;
        i = 3.5f;
    }
}
", CompilationException.ToDatapackLiteralsIntegerOnly,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region method call tests

        // A lot of the relevant tests are already done in
        /// <see cref="MCFunctionAttributeTests"/>
        // so here is just testing the remaining diagnostics and exceptions.
        // Also `in`, `out`, `ref`

        // Raw to test generated function call.
        [TestMethod]
        public void TestCall1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        CalledMethod();
    }

    public static void CalledMethod() { }
}
", @"
# (File (functions) compiled:internal/test.calledmethod.mcfunction)
# (Empty)

# (File (functions) compiled:test.testmethod.mcfunction)
function compiled:internal/test.calledmethod

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Ever since I've moved to my own System implementation, this error is
        // *surprisingly hard* to trigger. As in, "currently impossible". But
        // it will be possible in the future again. Hope I remember the
        // existence of FT0004 by then.
//        [TestMethod]
//        public void CallTestWrong2()
//            => TestCompilationFails(@"
//using MCMirror;
//public class Test {
//    [MCFunction]
//    public static void TestMethod() {
//        int i;
//        i = System.Math.Abs(0);
//    }
//}
//", "FT0004", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestCallInRefOutRaw()
            => TestCompilationSucceedsRaw(@"
public class Test {
    public static void TestMethod(int i, int j, int k, int l) {
        CalledMethod(i, in j, out k, ref l);
    }

    public static void CalledMethod(int i, in int j, out int k, ref int l) {
        i = j;
        k = 3;
        l = 4;
    }
}
", @"
# (File (functions) compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32.mcfunction)
scoreboard players operation #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg0 _ = #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg1 _
scoreboard players set #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg2 _ 3
scoreboard players set #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg3 _ 4

# (File (functions) compiled:internal/test.testmethod-int32-int32-int32-int32.mcfunction)
scoreboard players operation #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg0 _ = #compiled:internal/test.testmethod-int32-int32-int32-int32##arg0 _
scoreboard players operation #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg1 _ = #compiled:internal/test.testmethod-int32-int32-int32-int32##arg1 _
scoreboard players operation #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg3 _ = #compiled:internal/test.testmethod-int32-int32-int32-int32##arg3 _
function compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32
scoreboard players operation #compiled:internal/test.testmethod-int32-int32-int32-int32##arg2 _ = #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg2 _
scoreboard players operation #compiled:internal/test.testmethod-int32-int32-int32-int32##arg3 _ = #compiled:internal/test.calledmethod-int32-in-int32-out-int32-ref-int32##arg3 _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestOverloadsRaw()
            => TestCompilationSucceedsRaw(@"
public class Test {
    public static void TestMethod(int i) {
        Called();
        Called(i);
    }

    public static void Called() {}
    public static void Called(int i) {}
}
", @"
# (File (functions) compiled:internal/test.called.mcfunction)
# (Empty)

# (File (functions) compiled:internal/test.called-int32.mcfunction)
# (Empty)

# (File (functions) compiled:internal/test.testmethod-int32.mcfunction)
function compiled:internal/test.called
scoreboard players operation #compiled:internal/test.called-int32##arg0 _ = #compiled:internal/test.testmethod-int32##arg0 _
function compiled:internal/test.called-int32
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // TODO: It is currently impossible to test for staticness as there's
        // no support for objects/custom structs/built-in methods.
        /// <see cref="CompilationException.ToDatapackMethodCallsMustBeStatic"/>

        #endregion

        #region if-else tests
        // Raw to output of "unless" branches.
        [TestMethod]
        public void TestBranching1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 1;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 1

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test output of "if" branches.
        [TestMethod]
        public void TestBranching2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i == 0) {
            i = 1;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-eq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-eq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-eq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 1

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        // Raw to test output of larger block bodies.
        public void TestBranching3Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 1;
            i = 2;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run function compiled:test.testmethod-0-if-branch

# Method Attributes:
#   [MCFunction]

# (File (functions) compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 1
scoreboard players set #compiled:test.testmethod#i _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test the output when having both if and else.
        [TestMethod]
        public void TestBranching4Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i, j;
        i = 0;
        j = 0;
        if (i != 0) {
            j = 1;
        } else {
            j = 2;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#j _ 1
execute unless score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#j _ 2

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test both large bodies and having both if and else, and also using the same variable in both.
        [TestMethod]
        public void TestBranching5Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 1;
        } else {
            i = 2;
            i = 3;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 1
execute unless score #compiled:test.testmethod##IFTEMP0 _ matches 1 run function compiled:test.testmethod-1-if-branch

# Method Attributes:
#   [MCFunction]

# (File (functions) compiled:test.testmethod-1-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 2
scoreboard players set #compiled:test.testmethod#i _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to.. test arg names in branches I guess?
        [TestMethod]
        public void TestBranching6Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static void TestMethod(int i, int j) {
        if (i != 0) {
            j = 1;
        } else {
            j = 2;
            j = 3;
        }
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod-int32-int32.mcfunction)
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:internal/test.testmethod-int32-int32##arg0 _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:internal/test.testmethod-int32-int32##IFTEMP0 _ = #RET _
execute if score #compiled:internal/test.testmethod-int32-int32##IFTEMP0 _ matches 1 run scoreboard players set #compiled:internal/test.testmethod-int32-int32##arg1 _ 1
execute unless score #compiled:internal/test.testmethod-int32-int32##IFTEMP0 _ matches 1 run function compiled:internal/test.testmethod-int32-int32-1-if-branch

# (File (functions) compiled:internal/test.testmethod-int32-int32-1-if-branch.mcfunction)
scoreboard players set #compiled:internal/test.testmethod-int32-int32##arg1 _ 2
scoreboard players set #compiled:internal/test.testmethod-int32-int32##arg1 _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // The "let's throw against the wall and see what sticks" test.
        // Raw because it's chaos.
        // Status update: It's even worse with the function-call comparison! Yay!
        [TestMethod]
        public void TestBranching9Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i, j;
        i = 0;
        j = 0;
        if (i != 0) {
            j = 0;
            if (i != 0) {
                j = 2;
                j = 2;
            } else {
                j = 3;
                j = 3;
            }
            j = 0;
        } else {
            j = 1;
            if (i != 0) {
                j = 4;
                j = 4;
            } else {
                j = 5;
                j = 5;
            }
            j = 1;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run function compiled:test.testmethod-0-if-branch
execute unless score #compiled:test.testmethod##IFTEMP0 _ matches 1 run function compiled:test.testmethod-3-if-branch

# Method Attributes:
#   [MCFunction]

# (File (functions) compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP1 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP1 _ matches 1 run function compiled:test.testmethod-1-if-branch
execute unless score #compiled:test.testmethod##IFTEMP1 _ matches 1 run function compiled:test.testmethod-2-if-branch
scoreboard players set #compiled:test.testmethod#j _ 0

# (File (functions) compiled:test.testmethod-1-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 2
scoreboard players set #compiled:test.testmethod#j _ 2

# (File (functions) compiled:test.testmethod-2-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 3
scoreboard players set #compiled:test.testmethod#j _ 3

# (File (functions) compiled:test.testmethod-3-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 1
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP2 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP2 _ matches 1 run function compiled:test.testmethod-4-if-branch
execute unless score #compiled:test.testmethod##IFTEMP2 _ matches 1 run function compiled:test.testmethod-5-if-branch
scoreboard players set #compiled:test.testmethod#j _ 1

# (File (functions) compiled:test.testmethod-4-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 4
scoreboard players set #compiled:test.testmethod#j _ 4

# (File (functions) compiled:test.testmethod-5-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 5
scoreboard players set #compiled:test.testmethod#j _ 5
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test chained conditionals.
        [TestMethod]
        public void TestBranching10Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i, j, k;
        i = 0;
        j = 0;
        k = 0;
        if (i != 0) {
            if (j != 0) {
                if (k != 0) {
                    i = 1;
                }
            }
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players set #compiled:test.testmethod#k _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run function compiled:test.testmethod-0-if-branch

# Method Attributes:
#   [MCFunction]

# (File (functions) compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#j _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP1 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP1 _ matches 1 run function compiled:test.testmethod-1-if-branch

# (File (functions) compiled:test.testmethod-1-if-branch.mcfunction)
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#k _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP2 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP2 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 1
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test a non-1 variable???'s !=
        [TestMethod]
        public void TestBranching11Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i != 10) {
            i = 10;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 10
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 10

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // ANd a raw to test a non-1 variable's ==
        [TestMethod]
        public void TestBranching12Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i == 10) {
            i = 10;
        }
    }
}
", @"
# (File (functions) compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:internal/int.operator-eq-int32-int32##arg0 _ = #compiled:test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-eq-int32-int32##arg1 _ 10
function compiled:internal/int.operator-eq-int32-int32
scoreboard players operation #compiled:test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:test.testmethod##IFTEMP0 _ matches 1 run scoreboard players set #compiled:test.testmethod#i _ 10

# Method Attributes:
#   [MCFunction]
", new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region return tests
        // Raw for return const
        [TestMethod]
        public void TestReturn1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        return 3;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for return variable
        [TestMethod]
        public void TestReturn2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 3;
        return i;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 3
scoreboard players operation #RET _ = #compiled:internal/test.testmethod#i _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for return call, including not setting an unneeded #RET.
        [TestMethod]
        public void TestReturn3Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        return TestMethod2();
    }
    static int TestMethod2() {
        return 3;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.testmethod2

# (File (functions) compiled:internal/test.testmethod2.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // TODO: Regression: final return trees now result in nested function calls instead of just assignment.
        // It _works_ but is *far* from optimal and I'd really like the old behaviour.
        // Raw for no reason in particular, this could very well be a SucceedsTheSame.
        [TestMethod]
        public void TestReturn4Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i,j;
        i = 0;
        j = 0;
        if (i != 0) {
            if (j != 0) {
                return 1;
            } else {
                return 2;
            }
        } else {
            return 3;
        }
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 0
scoreboard players set #compiled:internal/test.testmethod#j _ 0
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:internal/test.testmethod#i _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:internal/test.testmethod##IFTEMP0 _ = #RET _
execute if score #compiled:internal/test.testmethod##IFTEMP0 _ matches 1 run function compiled:internal/test.testmethod-0-if-branch
execute if score #GOTOFLAG _ matches 0 run function compiled:internal/test.testmethod-4-if-branch
execute if score #GOTOFLAG _ matches 1 run scoreboard players set #GOTOFLAG _ 0

# (File (functions) compiled:internal/test.testmethod-0-if-branch.mcfunction)
scoreboard players operation #compiled:internal/int.operator-neq-int32-int32##arg0 _ = #compiled:internal/test.testmethod#j _
scoreboard players set #compiled:internal/int.operator-neq-int32-int32##arg1 _ 0
function compiled:internal/int.operator-neq-int32-int32
scoreboard players operation #compiled:internal/test.testmethod##IFTEMP1 _ = #RET _
execute if score #compiled:internal/test.testmethod##IFTEMP1 _ matches 1 run function compiled:internal/test.testmethod-1-if-branch
execute if score #GOTOFLAG _ matches 0 unless score #compiled:internal/test.testmethod##IFTEMP1 _ matches 1 run function compiled:internal/test.testmethod-3-if-branch

# (File (functions) compiled:internal/test.testmethod-1-if-branch.mcfunction)
scoreboard players set #RET _ 1
scoreboard players set #GOTOFLAG _ 1

# (File (functions) compiled:internal/test.testmethod-3-if-branch.mcfunction)
scoreboard players set #RET _ 2
scoreboard players set #GOTOFLAG _ 1

# (File (functions) compiled:internal/test.testmethod-4-if-branch.mcfunction)
execute unless score #compiled:internal/test.testmethod##IFTEMP0 _ matches 1 run function compiled:internal/test.testmethod-5-if-branch
execute if score #GOTOFLAG _ matches 1 run scoreboard players set #GOTOFLAG _ 0

# (File (functions) compiled:internal/test.testmethod-5-if-branch.mcfunction)
scoreboard players set #RET _ 3
scoreboard players set #GOTOFLAG _ 1
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for label name format I guess
        [TestMethod]
        public void TestReturnLabeledRaw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
    label:
        return 0;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #RET _ 0

# (File (functions) compiled:internal/test.testmethod-0-goto-label-1.mcfunction)
scoreboard players set #RET _ 0
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnWrong1()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        return 3;
        int i;
    }
}
", CompilationException.ToDatapackReturnNoNonReturnAfterReturn,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnWrong5()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            return i;
            i = 3;
        } else {
            return 4;
        }
    }
}
", CompilationException.ToDatapackReturnNoNonReturnAfterReturn,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region variable name tests
        // Raw for name format
        [TestMethod]
        public void TestNames1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod(int i) {
        return i;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod-int32.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.testmethod-int32##arg0 _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for name format II
        // (update: ew needs constructor)
        [TestMethod]
        public void TestNames2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int nUmBeR;
    static int TestMethod() {
        return nUmBeR;
    }
}
", @"
# (File (functions) compiled:internal/test.-constructstatic-.mcfunction)
scoreboard players set #compiled:test#nUmBeR _ 0

# Method Attributes:
#   [MCMirror.TrueLoad]

# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #RET _ = #compiled:test#nUmBeR _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for complex names in statements
        [TestMethod]
        public void TestNames3Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
namespace TestSpace {
    internal class Test {
        internal class InnerClass {
            static int TestMethod() {
                int nUmBeR;
                nUmBeR = 3;
                return nUmBeR;
            }
        }
    }
}
", @"
# (File (functions) compiled:internal/testspace.test.innerclass.testmethod.mcfunction)
scoreboard players set #compiled:internal/testspace.test.innerclass.testmethod#nUmBeR _ 3
scoreboard players operation #RET _ = #compiled:internal/testspace.test.innerclass.testmethod#nUmBeR _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for.. some reason
        [TestMethod]
        public void TestNames4Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static void TestMethod() {
        int i;
        i = 3;
        TestMethod2(i);
        TestMethod2(3);
    }

    static int TestMethod2(int number) { return 3; }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 3
scoreboard players operation #compiled:internal/test.testmethod2-int32##arg0 _ = #compiled:internal/test.testmethod#i _
function compiled:internal/test.testmethod2-int32
scoreboard players set #compiled:internal/test.testmethod2-int32##arg0 _ 3
function compiled:internal/test.testmethod2-int32

# (File (functions) compiled:internal/test.testmethod2-int32.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region struct tests
        // Raw for struct format
        [TestMethod]
        public void StructAccessTest1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static void TestMethod(int i) {
        int3 pos;
        pos.x = 2;
        pos.y = i;
        pos.z = 0;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod-int32.mcfunction)
scoreboard players set #compiled:internal/test.testmethod-int32#pos#x _ 2
scoreboard players operation #compiled:internal/test.testmethod-int32#pos#y _ = #compiled:internal/test.testmethod-int32##arg0 _
scoreboard players set #compiled:internal/test.testmethod-int32#pos#z _ 0
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        // Raw for yet more struct format
        public void StructAccessTest2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int TestMethod(int3 pos) {
        return pos.z;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod-int3.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.testmethod-int3##arg0#z _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw for very nested struct format
        // Update: ouch the constructors added
        [TestMethod]
        public void StructAssignTest1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static void TestMethod(Lorem a, Lorem b) {
        a = b;
    }
}
struct Lorem {
    Ipsum ipsum;
    int val;
    DolorSitAmet dolorSitAmet;
}
struct Ipsum {
    DolorSitAmet dolorSitAmet2;
}
struct DolorSitAmet {
    int3 pos;
    int w;
}
", @"
# (File (functions) compiled:internal/dolorsitamet.-construct-.mcfunction)
scoreboard players set #RET#pos#x _ 0
scoreboard players set #RET#pos#y _ 0
scoreboard players set #RET#pos#z _ 0
scoreboard players set #RET#w _ 0

# (File (functions) compiled:internal/ipsum.-construct-.mcfunction)
scoreboard players set #RET#dolorSitAmet2#pos#x _ 0
scoreboard players set #RET#dolorSitAmet2#pos#y _ 0
scoreboard players set #RET#dolorSitAmet2#pos#z _ 0
scoreboard players set #RET#dolorSitAmet2#w _ 0

# (File (functions) compiled:internal/lorem.-construct-.mcfunction)
scoreboard players set #RET#ipsum#dolorSitAmet2#pos#x _ 0
scoreboard players set #RET#ipsum#dolorSitAmet2#pos#y _ 0
scoreboard players set #RET#ipsum#dolorSitAmet2#pos#z _ 0
scoreboard players set #RET#ipsum#dolorSitAmet2#w _ 0
scoreboard players set #RET#val _ 0
scoreboard players set #RET#dolorSitAmet#pos#x _ 0
scoreboard players set #RET#dolorSitAmet#pos#y _ 0
scoreboard players set #RET#dolorSitAmet#pos#z _ 0
scoreboard players set #RET#dolorSitAmet#w _ 0

# (File (functions) compiled:internal/test.testmethod-lorem-lorem.mcfunction)
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#ipsum#dolorSitAmet2#pos#x _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#ipsum#dolorSitAmet2#pos#x _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#ipsum#dolorSitAmet2#pos#y _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#ipsum#dolorSitAmet2#pos#y _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#ipsum#dolorSitAmet2#pos#z _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#ipsum#dolorSitAmet2#pos#z _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#ipsum#dolorSitAmet2#w _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#ipsum#dolorSitAmet2#w _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#val _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#val _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#dolorSitAmet#pos#x _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#dolorSitAmet#pos#x _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#dolorSitAmet#pos#y _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#dolorSitAmet#pos#y _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#dolorSitAmet#pos#z _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#dolorSitAmet#pos#z _
scoreboard players operation #compiled:internal/test.testmethod-lorem-lorem##arg0#dolorSitAmet#w _ = #compiled:internal/test.testmethod-lorem-lorem##arg1#dolorSitAmet#w _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test struct member assignment
        [TestMethod]
        public void StructAssignTest2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int3 TestMethod() {
        int3 pos;
        pos.x = 1;
        pos.y = 2;
        pos.z = 3;
        return pos;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#pos#x _ 1
scoreboard players set #compiled:internal/test.testmethod#pos#y _ 2
scoreboard players set #compiled:internal/test.testmethod#pos#z _ 3
scoreboard players operation #RET#x _ = #compiled:internal/test.testmethod#pos#x _
scoreboard players operation #RET#y _ = #compiled:internal/test.testmethod#pos#y _
scoreboard players operation #RET#z _ = #compiled:internal/test.testmethod#pos#z _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test method+partial struct assignment
        [TestMethod]
        public void StructAssignTest3Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static int3 TestMethod() {
        int3 pos;
        pos.x = 1;
        pos.y = 2;
        pos.z = 3;
        return pos;
    }

    static void TestMethod2() {
        int3 pos;
        pos = TestMethod();
        pos.z = 4;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#pos#x _ 1
scoreboard players set #compiled:internal/test.testmethod#pos#y _ 2
scoreboard players set #compiled:internal/test.testmethod#pos#z _ 3
scoreboard players operation #RET#x _ = #compiled:internal/test.testmethod#pos#x _
scoreboard players operation #RET#y _ = #compiled:internal/test.testmethod#pos#y _
scoreboard players operation #RET#z _ = #compiled:internal/test.testmethod#pos#z _

# (File (functions) compiled:internal/test.testmethod2.mcfunction)
function compiled:internal/test.testmethod
scoreboard players operation #compiled:internal/test.testmethod2#pos#x _ = #RET#x _
scoreboard players operation #compiled:internal/test.testmethod2#pos#y _ = #RET#y _
scoreboard players operation #compiled:internal/test.testmethod2#pos#z _ = #RET#z _
scoreboard players set #compiled:internal/test.testmethod2#pos#z _ 4
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // Raw to test non-= asignment
        [TestMethod]
        public void StructAssignTest4Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
internal class Test {
    static void TestMethod(int3 a) {
        a.x = 24;
        a.y += 23;
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod-int3.mcfunction)
scoreboard players set #compiled:internal/test.testmethod-int3##arg0#x _ 24
scoreboard players operation #compiled:internal/int.operator-add-int32-int32##arg0 _ = #compiled:internal/test.testmethod-int3##arg0#y _
scoreboard players set #compiled:internal/int.operator-add-int32-int32##arg1 _ 23
function compiled:internal/int.operator-add-int32-int32
scoreboard players operation #compiled:internal/test.testmethod-int3##arg0#y _ = #RET _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void StructWrongTest1()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static Wrong2 TestMethod(Wrong2 w) {
        return w;
    }
}

struct Wrong1 {
    float i;
}
struct Wrong2 {
    Wrong1 wrong;
}
", CompilationException.ToDatapackStructsMustEventuallyInt,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region run raw tests
        // Raw because there's no way to get this mcfunction code otherwise
        [TestMethod]
        public void TestRunRaw1Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod() {
        Run(""say hoi"");
    }
}
", @"
# (File (functions) compiled:internal/test.testmethod.mcfunction)
say hoi
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestRunRaw2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod() {
        Run(""/say hoi"");
    }
}
", @"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod() {
        Run(""say hoi"");
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestRunRawWrong1()
            => TestCompilationFails(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod(int i) {
        Run($""{i}"");
    }
}
", "FT0010", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestRunRawWrong2()
            => TestCompilationFails(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    const string hoi = ""say hoi"";
    static void TestMethod(int i) {
        Run(hoi);
    }
}
", "FT0005", new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region [NoCompile] tests
        [TestMethod]
        public void TestNoCompile1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror.Internal;
internal class Test {
    [NoCompile]
    static int TestMethod1() {
        return 1;
    }

    static int TestMethod2() {
        return 2;
    }
    [NoCompile]
    static int TestMethod3() {
        return 3;
    }
}
", @"
internal class Test {
    static int TestMethod2() {
        return 2;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestNoCompile2Raw()
            => TestCompilationSucceedsRaw(@"
using MCMirror.Internal;
[NoCompile]
internal class Test {
    static int TestMethod1() {
        return 1;
    }

    static int TestMethod2() {
        return 2;
    }

    static int TestMethod3() {
        return 3;
    }
}
", @"
", new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion
    }
}