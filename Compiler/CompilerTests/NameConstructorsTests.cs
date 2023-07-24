using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class NameConstructorsTests {

        [TestMethod]
        public void StaticifyTest1()
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

    }
}