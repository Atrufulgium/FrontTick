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
", "# (File compiled:test.testmethod.mcfunction)",
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
", "# (File compiled:test.testmethod.mcfunction)",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void WrongDeclarationTest1()
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
        public void WrongDeclarationTest2()
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

        // TODO: If-statements (or any other scoping mechanism) aren't implemented yet so this will fail
        [TestMethod]
        public void WrongDeclarationTest3()
            => TestCompilationThrows(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        if (true) {
            int i;
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
scoreboard players operation #compiled:test.testmethod#i _ %= #CONST#5 _",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

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
scoreboard players operation #compiled:test.testmethod#i _ %= #compiled:test.testmethod#j _",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // TODO: Test for RHS method calls. This requires first implementing methods though.
        // TODO: Support and test for negative integer literals.

        [TestMethod]
        public void WrongAssignmentTest1()
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
        public void WrongAssignmentTest2()
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
        public void WrongCallTest1()
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
        public void WrongCallTest2()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
        int i;
        i = System.Math.Abs(0);
    }
}
", "FT0004",
                new IFullVisitor[] { new ProcessedToDatapackWalker() });

        // TODO: It is currently impossible to test for staticness as there's
        // no support for objects/custom structs/built-in methods.
        /// <see cref="CompilationException.ToDatapackMethodCallsMustBeStatic"/>

        #endregion

    }
}