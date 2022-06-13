using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass()]
    public class CompilerTests {
        [TestMethod()]
        public void CompileTest() {
            // Yes people browsing git histories to the furthest past, this is
            // not how tests work. I know. I don't care when debugging. Go to
            // the present where they *are* proper.
            // (Okay no one is actually gonna read this.)
            Compiler.Compile(@"
using MCMirror;
public class Test {
    [MCFunction]
    public static void TestMethod() {
    }
    public static void TestMethod2() {
    }
    [TestMethod()]
    public static void TestMethod3() {
    }
    [MCMirror.MCFunction]
    public static void TestMethod4() {
    }
}
");
            Assert.Fail();
        }
    }
}