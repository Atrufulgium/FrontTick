using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class ArrowRewriterTests {

        [TestMethod]
        public void MethodTest1()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int GetRandomNumber() => 4;
}
", @"
public class Test {
    public static int GetRandomNumber() {
        return 4;
    }
}
");

        [TestMethod]
        public void MethodTest2()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int i;
    public static void Mutate() => i += 3;
}
", @"
public class Test {
    public static int i;
    public static void Mutate() {
        i += 3;
    }
}
");

        [TestMethod]
        public void PropertyTest1()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int RandomNumber => 4;
}
", @"
public class Test {
    public static int RandomNumber { get => 4; }
}
");

        [TestMethod]
        public void PropertyTest2()
            => TestCompilationSucceedsTheSame(@"
public class Test {
    public static int RandomNumber { get => 4; }
}
", @"
public class Test {
    public static int RandomNumber { get => { return 4;} }
}
");
    }
}