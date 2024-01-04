using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class StaticifyInstanceTests {

        [TestMethod]
        public void StaticifyTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    static void TestMethod(Test t) {
        t.InstanceMethod();
    }

    void InstanceMethod() {}
}
", @"
internal struct Test {
    int val;

    static void TestMethod(Test t) {
        STATICーInstanceMethod(ref t);
    }

    static void STATICーInstanceMethod(ref Test t) {}
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void StaticifyTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceMethod() {
        return this.val;
    }
}
", @"
internal struct Test {
    int val;

    static int STATICーInstanceMethod(ref Test t) {
        return t.val;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void StaticifyTest3()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceMethod() {
        return this.InstanceMethod();
    }
}
", @"
internal struct Test {
    int val;

    static int STATICーInstanceMethod(ref Test t) {
        return Test.STATICーInstanceMethod(ref t);
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void StaticifyTest4()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceProperty { get => this.val; set => this.val = value; }
}
", @"
internal struct Test {
    int val;
    
    static int STATICーGetーInstanceProperty(ref Test t) => t.val;
    static void STATICーSetーInstanceProperty(ref Test t, int value) => t.val = value;
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void ThisTest1()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceMethod() {
        return val;
    }
}
", @"
internal struct Test {
    int val;

    int InstanceMethod() {
        return this.val;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void ThisTest2()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceMethod() {
        return InstanceMethod();
    }
}
", @"
internal struct Test {
    int val;

    int InstanceMethod() {
        return this.InstanceMethod();
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

        [TestMethod]
        public void ThisTest3()
            => TestCompilationSucceedsTheSame(@"
internal struct Test {
    int val;

    int InstanceProperty { get => val; set => val = value; }

    int InstanceMethod() {
        return InstanceProperty;
    }
}
", @"
internal struct Test {
    int val;

    int InstanceProperty { get => val; set => val = value; }

    int InstanceMethod() {
        return this.InstanceProperty;
    }
}
", new IFullVisitor[] { new ProcessedToDatapackWalker() });

    }
}