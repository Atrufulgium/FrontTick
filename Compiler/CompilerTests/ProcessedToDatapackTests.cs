﻿using Atrufulgium.FrontTick.Compiler.Visitors;
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

        // TODO: Test for RHS method calls. This requires first implementing methods though.
        // TODO: Support and test for negative integer literals.

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
        if (i != 0)
            i = 1;
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
        int i;
        i = 0;
        if (i != 0) {
            i = 1;
        } else {
            i = 2;
        }
    }
}
", @"
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
execute if score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 2
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
execute unless score #compiled:test.testmethod#i _ matches 0 run scoreboard players set #compiled:test.testmethod#i _ 1
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-1-else-branch

# (File compiled:test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 2
scoreboard players set #compiled:test.testmethod#i _ 3
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching6()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 0;
            if (i != 0) {
                i = 2;
                i = 2;
            } else {
                i = 3;
                i = 3;
            }
            i = 0;
        } else {
            i = 1;
            if (i != 0) {
                i = 4;
                i = 4;
            } else {
                i = 5;
                i = 5;
            }
            i = 1;
        }
    }
}
", @"
# (File compiled:test.testmethod.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-0-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-1-else-branch

# (File compiled:test.testmethod-0-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 0
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-2-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-3-else-branch
scoreboard players set #compiled:test.testmethod#i _ 0

# (File compiled:test.testmethod-1-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 1
execute unless score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-4-if-branch
execute if score #compiled:test.testmethod#i _ matches 0 run function compiled:test.testmethod-5-else-branch
scoreboard players set #compiled:test.testmethod#i _ 1

# (File compiled:test.testmethod-2-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 2
scoreboard players set #compiled:test.testmethod#i _ 2

# (File compiled:test.testmethod-3-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 3
scoreboard players set #compiled:test.testmethod#i _ 3

# (File compiled:test.testmethod-4-if-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 4
scoreboard players set #compiled:test.testmethod#i _ 4

# (File compiled:test.testmethod-5-else-branch.mcfunction)
scoreboard players set #compiled:test.testmethod#i _ 5
scoreboard players set #compiled:test.testmethod#i _ 5
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestBranching7()
            => TestCompilationSucceeds(@"
using MCMirror;
internal class Test {
    [MCFunction]
    static void TestMethod() {
        int i, j, k;
        i = 0;
        j = 0;
        k = 0;
        if (i != 0)
            if (j != 0)
                if (k != 0)
                    i = 1;
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

        #region root return tests
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
        if (i != 0)
            if (j != 0)
                return 1;
            else
                return 2;
        else
            return 3;
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
", CompilationException.ToDatapackReturnMustBeIdentifierOrLiteralsOrCalls,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnWrong3()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 0;
        if (i != 0)
            return 3;
        return 4;
    }
}
", CompilationException.ToDatapackReturnIfMustAlsoHaveReturnElse,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnWrong4()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 3;
            return i;
        } else {
            return 4;
        }
    }
}
", CompilationException.ToDatapackReturnBranchMustBeReturnStatement,
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

        [TestMethod]
        public void TestReturnWrong6()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            return i;
        } else {
            i = 3;
        }
        return i;
    }
}
", CompilationException.ToDatapackReturnNoNonReturnAfterReturn,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void TestReturnWrong7()
            => TestCompilationThrows(@"
using MCMirror;
internal class Test {
    static int TestMethod() {
        int i;
        i = 0;
        if (i != 0) {
            i = 3;
        } else {
            return i;
        }
        return i;
    }
}
", CompilationException.ToDatapackReturnElseMustAlsoHaveReturnIf,
                new IFullVisitor[] { new ProcessedToDatapackWalker() });
        #endregion
    }
}