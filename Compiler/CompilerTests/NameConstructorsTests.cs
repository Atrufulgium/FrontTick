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

        [TestMethod]
        public void NameStaticConstructorTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static int val;

    static Test() {
        val = 3;
    }
}
", @"
internal struct Test {
    static int val;

    [MCMirror.TrueLoad]
    static void CONSTRUCTSTATIC() {
        val = 3;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameStaticConstructorTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static int val = 3;
}
", @"
internal struct Test {
    static int val;

    static Test() {
        val = 3;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameStaticConstructorTest3()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static int val1 = 10;
    static int val2;

    static Test() {
        val2 = 20;
    }
}
", @"
internal struct Test {
    static int val1;
    static int val2;

    static Test() {
        val1 = 10;
        val2 = 20;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void NameStaticAndNormalConstructorTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    static int val1 = 10;
    static int val2;
    int val3 = 30;
    int val4;

    static Test() {
        val2 = 20;
    }

    Test(int i) {
        val4 = i;
    }
}
", @"
internal struct Test {
    static int val1;
    static int val2;
    int val3;
    int val4;

    static Test() {
        val1 = 10;
        val2 = 20;
    }

    Test(int i) {
        val3 = 30;
        val4 = i;
    }

    public Test() {
        val3 = 30;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}