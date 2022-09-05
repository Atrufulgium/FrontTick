using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class ProcessedToDatapackTests {

        #region declaration tests
        [TestMethod]
        public void DeclarationTest1()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
    }
}
", "# (File compiled:test.testmethod.mcfunction)\n# (Empty)",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void DeclarationTest2()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i, j;
        int k;
    }
}
", "# (File compiled:test.testmethod.mcfunction)\n# (Empty)",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

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

        [TestMethod]
        public void DeclarationTestWrong3()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            int j;
            j = 0;
        }
    }
}
", CompilationException.ToDatapackDeclarationsMustBeInMethodRootScope,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region assignment tests
        [TestMethod]
        public void AssignmentTest1()
            => TestCompilationSucceeds(@"
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
", @"# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation #compiled:test.testmethod#i _ += #CONST#1 _
scoreboard players operation #compiled:test.testmethod#i _ -= #CONST#2 _
scoreboard players operation #compiled:test.testmethod#i _ *= #CONST#3 _
scoreboard players operation #compiled:test.testmethod#i _ /= #CONST#4 _
scoreboard players operation #compiled:test.testmethod#i _ %= #CONST#5 _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTest2()
            => TestCompilationSucceeds(@"
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
", @"# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players operation #compiled:test.testmethod#i _ = #compiled:test.testmethod#j _
scoreboard players operation #compiled:test.testmethod#i _ += #compiled:test.testmethod#j _
scoreboard players operation #compiled:test.testmethod#i _ -= #compiled:test.testmethod#j _
scoreboard players operation #compiled:test.testmethod#i _ *= #compiled:test.testmethod#j _
scoreboard players operation #compiled:test.testmethod#i _ /= #compiled:test.testmethod#j _
scoreboard players operation #compiled:test.testmethod#i _ %= #compiled:test.testmethod#j _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // (This test also depends on "return" working properly.)
        [TestMethod]
        public void AssignmentTest3()
            => TestCompilationSucceeds(@"
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
# (File compiled:internal/test.getthree.mcfunction)
scoreboard players set #RET _ 3

# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 3
function compiled:internal/test.getthree
scoreboard players operation #compiled:test.testmethod#i _ -= #RET _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTest4()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 3
scoreboard players operation #compiled:test.testmethod#i _ += #CONST#-3 _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTestWrong1()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 0;
        i |= 1;
    }
}
", CompilationException.ToDatapackAssignmentOpsMustBeSimpleOrArithmetic,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTestWrong2()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = 2 + 2;
    }
}
", CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void AssignmentTestWrong3()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = -(2 + 2);
    }
}
", CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

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
        [TestMethod]
        public void TestCall1()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        CalledMethod();
    }

    public static void CalledMethod() { }
}
", @"
# (File compiled:internal/test.calledmethod.mcfunction)
# (Empty)

# (File compiled:test.testmethod.mcfunction)
function compiled:internal/test.calledmethod
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void CallTestWrong1()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        CalledMethod(2 + 2);
    }

    public static void CalledMethod(int i) { }
}
", CompilationException.ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void CallTestWrong2()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = System.Math.Abs(0);
    }
}
", "FT0004", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // TODO: It is currently impossible to test for staticness as there's
        // no support for objects/custom structs/built-in methods.
        /// <see cref="CompilationException.ToDatapackMethodCallsMustBeStatic"/>

        #endregion

        #region if-else tests
        [TestMethod]
        public void TestBranching1()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching2()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching3()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-0-if-branch

# (File compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 1
scoreboard players set #compiled:test.testmethod#i _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching4()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#j _ 1
execute if score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#j _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching5()
            => TestCompilationSucceeds(@"
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
 # (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players operation conditionIdentifier-2 _ = #compiled:test.testmethod#i _
execute unless score conditionIdentifier-2 _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
execute if score conditionIdentifier-2 _ matches 0 run function compiled:test.testmethod-1-else-branch

# (File compiled:test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 2
scoreboard players set #compiled:test.testmethod#i _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching6()
            => TestCompilationSucceeds(@"
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
 # (File compiled:internal/test.testmethod.mcfunction)
execute unless score #compiled:internal/test.testmethod##arg0 _ matches 0 run scoreboard players set #compiled:internal/test.testmethod##arg1 _ 1
execute if score #compiled:internal/test.testmethod##arg0 _ matches 0 run function compiled:internal/test.testmethod-1-else-branch

# (File compiled:internal/test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:internal/test.testmethod##arg1 _ 2
scoreboard players set #compiled:internal/test.testmethod##arg1 _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching7()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static void TestMethod(int i) {
        if (i != 0) {
            i = 1;
        } else {
            i = 2;
        }
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation conditionIdentifier-2 _ = #compiled:internal/test.testmethod##arg0 _
execute unless score conditionIdentifier-2 _ matches 0 run scoreboard players set #compiled:internal/test.testmethod##arg0 _ 1
execute if score conditionIdentifier-2 _ matches 0 run scoreboard players set #compiled:internal/test.testmethod##arg0 _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching8()
            => TestCompilationSucceeds(@"
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
            j = 3;
        }
    }
}
", @"
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#j _ 1
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-1-else-branch

# (File compiled:test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 2
scoreboard players set #compiled:test.testmethod#j _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching9()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-0-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-1-else-branch

# (File compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-2-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-3-else-branch
scoreboard players set #compiled:test.testmethod#j _ 0

# (File compiled:test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 1
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-4-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-5-else-branch
scoreboard players set #compiled:test.testmethod#j _ 1

# (File compiled:test.testmethod-2-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 2
scoreboard players set #compiled:test.testmethod#j _ 2

# (File compiled:test.testmethod-3-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 3
scoreboard players set #compiled:test.testmethod#j _ 3

# (File compiled:test.testmethod-4-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 4
scoreboard players set #compiled:test.testmethod#j _ 4

# (File compiled:test.testmethod-5-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#j _ 5
scoreboard players set #compiled:test.testmethod#j _ 5
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching10()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
scoreboard players set #compiled:test.testmethod#j _ 0
scoreboard players set #compiled:test.testmethod#k _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 unless score #compiled:test.testmethod#j _ matches 0 unless score #compiled:test.testmethod#k _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching11()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 10 run scoreboard players set #compiled:test.testmethod#i _ 10
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching12()
            => TestCompilationSucceeds(@"
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
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute if score #compiled:test.testmethod#i _ matches 10 run scoreboard players set #compiled:test.testmethod#i _ 10
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void BranchingTestWrong1()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i > 0)
            i = 1;
    }
}
", CompilationException.ToDatapackIfConditionalMustBeIdentifierNotEqualToZero);

        [TestMethod]
        public void BranchingTestWrong2()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (HiMayIHaveSomeTruth())
            i = 1;
    }

    static bool HiMayIHaveSomeTruth() { return true; }
}
", CompilationException.ToDatapackIfConditionalMustBeIdentifierNotEqualToZero);
        #endregion

        #region return tests
        [TestMethod]
        public void TestReturn1()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        return 3;
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturn2()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 3;
        return i;
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 3
scoreboard players operation #RET _ = #compiled:internal/test.testmethod#i _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturn3()
            => TestCompilationSucceeds(@"
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
# (File compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.testmethod2

# (File compiled:internal/test.testmethod2.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturn4()
            => TestCompilationSucceeds(@"
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
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 0
scoreboard players set #compiled:internal/test.testmethod#j _ 0
execute unless score #compiled:internal/test.testmethod#i _ matches 0 run function compiled:internal/test.testmethod-0-if-branch
execute if score #compiled:internal/test.testmethod#i _ matches 0 run scoreboard players set #RET _ 3

# (File compiled:internal/test.testmethod-0-if-branch.mcfunction)
execute unless score #compiled:internal/test.testmethod#j _ matches 0 run scoreboard players set #RET _ 1
execute if score #compiled:internal/test.testmethod#j _ matches 0 run scoreboard players set #RET _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnLabeled()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
    label:
        return 0;
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
function compiled:internal/test.testmethod-0-goto-label-1

# (File compiled:internal/test.testmethod-0-goto-label-1.mcfunction)
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
        public void TestReturnWrong2()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        return 2 + 2;
    }
}
", CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls,
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
        [TestMethod]
        public void TestNames1()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static int TestMethod(int i) {
        return i;
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #RET _ = #compiled:internal/test.testmethod##arg0 _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestNames2()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    static int nUmBeR;
    static int TestMethod() {
        return nUmBeR;
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players operation #RET _ = #compiled:test#nUmBeR _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestNames3()
            => TestCompilationSucceeds(@"
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
# (File compiled:internal/testspace.test.innerclass.testmethod.mcfunction)
scoreboard players set #compiled:internal/testspace.test.innerclass.testmethod#nUmBeR _ 3
scoreboard players operation #RET _ = #compiled:internal/testspace.test.innerclass.testmethod#nUmBeR _
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestNames4()
            => TestCompilationSucceeds(@"
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
# (File compiled:internal/test.testmethod.mcfunction)
scoreboard players set #compiled:internal/test.testmethod#i _ 3
scoreboard players operation #compiled:internal/test.testmethod2##arg0 _ = #compiled:internal/test.testmethod#i _
function compiled:internal/test.testmethod2
scoreboard players set #compiled:internal/test.testmethod2##arg0 _ 3
function compiled:internal/test.testmethod2

# (File compiled:internal/test.testmethod2.mcfunction)
scoreboard players set #RET _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion

        #region run raw tests
        [TestMethod]
        public void TestRunRaw1()
            => TestCompilationSucceeds(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod() {
        Run(""say hoi"");
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
say hoi
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestRunRaw2()
            => TestCompilationSucceeds(@"
using MCMirror;
using static MCMirror.Internal.RawMCFunction;
internal class Test {
    static void TestMethod() {
        Run(""/say hoi"");
    }
}
", @"
# (File compiled:internal/test.testmethod.mcfunction)
say hoi
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
", "FT0005", new IFullVisitor[] { new ProcessedToDatapackWalker() });

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
            => TestCompilationSucceeds(@"
using MCMirror;
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
# (File compiled:internal/test.testmethod2.mcfunction)
scoreboard players set #RET _ 2
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestNoCompile2()
            => TestCompilationSucceeds(@"
using MCMirror;
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