using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameConstructorsTests {

        [TestMethod]
        public void NameConstructorTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    public Test(int value) {
        val = value;
    }

    static Test TestMethod() {
        return new Test(230);
    }
}
", @"
internal struct Test {
    int val;

    public static Test CONSTRUCT(int value) {
        Test created;
        created.val = value;
        return created;
    }

    static Test TestMethod() {
        return Test.CONSTRUCT(230);
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameConstructorTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val = 3;

    public Test() {}
}
", @"
internal struct Test {
    int val;

    public Test() {
        val = 3;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}