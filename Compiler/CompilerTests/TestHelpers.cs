using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    internal static class TestHelpers {

        public static void TestCompilationSucceeds(string source, string output) {
            Compiler compiler = new Compiler();
            compiler.Compile(source);
            Assert.IsTrue(compiler.CompilationSucceeded);
            Assert.AreEqual(output, compiler.CompiledDatapack.ToString());
        }

        public static void TestCompilationSucceeds(string[] sources, string output) {
            Compiler compiler = new Compiler();
            compiler.Compile(sources);
            Assert.IsTrue(compiler.CompilationSucceeded);
            output = output.Replace("\r\n", "\n");
            Assert.AreEqual(output, compiler.CompiledDatapack.ToString());
        }

        public static void TestCompilationFails(string source, string[] errorCodes) {
            Compiler compiler = new Compiler();
            compiler.Compile(source);
            Assert.IsFalse(compiler.CompilationSucceeded);

            List<string> observedErrors = new();
            foreach (var error in compiler.ErrorDiagnostics)
                observedErrors.Add(error.Id);

            CollectionAssert.AreEquivalent(errorCodes, observedErrors);
        }

    }
}
