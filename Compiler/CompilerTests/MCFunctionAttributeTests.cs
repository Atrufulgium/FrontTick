using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class MCFunctionAttributeTests {

        [TestMethod]
        public void MCFunctionTagTest1()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() { }
}
", "# (File compiled:test.testmethod.mcfunction)");

        [TestMethod]
        public void MCFunctionTagTest2()
            => TestCompilationSucceeds(@"
public class Test {
    [MCMirror.MCFunction]
    public static void TestMethod() { }
}
", "# (File compiled:test.testmethod.mcfunction)");

        [TestMethod]
        public void MCFunctionTagTest3()
            => TestCompilationSucceeds(new[] { @"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() { }
    [MCFunction]
    public static void AnotherTestMethod() { }
}
public class AnotherTest {
    [MCFunction]
    public static void YetAnotherTestMethod() { }
}
", @"
using MCMirror;
public class InAnotherFile {
    [MCFunction]
    public static void AnotherFileTestMethod() { }
}
" },
@"# (File compiled:anothertest.yetanothertestmethod.mcfunction)

# (File compiled:inanotherfile.anotherfiletestmethod.mcfunction)

# (File compiled:test.anothertestmethod.mcfunction)

# (File compiled:test.testmethod.mcfunction)
");

        [TestMethod]
        public void MCFunctionTagTest4()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [MCFunction(""do-test"")]
    public static void TestMethod() { }
}
", "# (File compiled:do-test.mcfunction)");

        [TestMethod]
        public void NoMCFunctionTagTest1()
            => TestCompilationSucceeds(@"
public class Test {
    public static void TestMethod() { }
}
", "# (File compiled:internal/test.testmethod.mcfunction)");

        [TestMethod]
        public void NoMCFunctionTagTest2()
            => TestCompilationSucceeds(@"
using MCMirror;
public class Test {
    [NBT(""This is a different, non-MCFunction attribute."")]
    public static void TestMethod() { }
}
", "# (File compiled:internal/test.testmethod.mcfunction)");

        [TestMethod]
        public void WrongMCFunctionTagTest1()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public void TestMethod() { }
}
", "FT0001");

        [TestMethod]
        public void WrongMCFunctionTagTest2()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static int TestMethod() { return 3; }
}
", "FT0001");

        [TestMethod]
        public void WrongMCFunctionTagTest3()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod(int i) { i += 3; }
}
", "FT0001");

        [TestMethod]
        public void WrongMCFunctionTagTest4()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction(""Do Some Test!"")]
    public static void TestMethod() { }
}
", "FT0002");

        [TestMethod]
        public void WrongMCFunctionTagTest5()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction("""")]
    public static void TestMethod() { }
}
", "FT0002");

        [TestMethod]
        public void WrongMCFunctionTagTest6()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction(""lorem-ipsum"")]
    public static void TestMethod() { }

    [MCFunction(""lorem-ipsum"")]
    public static void TestMethod2() { }
}
", "FT0003");

        [TestMethod]
        public void WrongMCFunctionTagTest7()
            => TestCompilationFails(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() { }

    [MCFunction]
    public static void tESTmETHOD() { }
}
", "FT0003");

    }
}