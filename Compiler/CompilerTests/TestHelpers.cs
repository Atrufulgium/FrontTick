using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    internal static class TestHelpers {

        public static void TestCompilationSucceeds(string source, string output, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationSucceeds(new[] { source }, output, compilationPhases);

        public static void TestCompilationSucceeds(string[] sources, string output, IEnumerable<IFullVisitor>? compilationPhases = null) {
            Compiler compiler = new();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);
            compiler.Compile(sources);
            try {
                Assert.IsTrue(compiler.CompilationSucceeded);
            } catch (AssertFailedException e) {
                Console.WriteLine("There were compilation errors:");
                foreach (var d in compiler.ErrorDiagnostics)
                    Console.WriteLine(d);
                throw e;
            }
            // Normalize the string to proper newlines, removed whitespace, and
            // a single newline before/after to make the test output readable.
            output = output.Replace("\r\n", "\n").Trim();
            output = $"\n{output}\n";
            var actual = compiler.CompiledDatapack.ToString().Replace("\r\n", "\n").Trim();
            actual = $"\n{actual}\n";
            try {
                Assert.AreEqual(output, actual);
            } catch (AssertFailedException e) {
                Console.WriteLine("Resulting code in tree-form:");
                Console.WriteLine(compiler.CompiledDatapack.ToTreeString());
                throw e;
            }
        }

        public static void TestCompilationFails(string source, string errorCode, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationFails(new[] { source }, new[] { errorCode }, compilationPhases);

        public static void TestCompilationFails(string[] sources, string errorCode, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationFails(sources, new[] { errorCode }, compilationPhases);

        public static void TestCompilationFails(string source, string[] errorCodes, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationFails(new[] { source }, errorCodes, compilationPhases);

        public static void TestCompilationFails(string[] sources, string[] errorCodes, IEnumerable<IFullVisitor>? compilationPhases = null) {
            Compiler compiler = new();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);
            compiler.Compile(sources);
            try {
                Assert.IsFalse(compiler.CompilationSucceeded, "Compilation succeeded instead of failing!");
            } catch (AssertFailedException e) {
                Console.WriteLine("Compilation accidentally succeeded! Resulting code:");
                Console.WriteLine(compiler.CompiledDatapack.ToString());
                Console.WriteLine("\n\nResulting code in tree-form:");
                Console.WriteLine(compiler.CompiledDatapack.ToTreeString());
                throw e;
            }

            List<string> observedErrors = new();
            foreach (var error in compiler.ErrorDiagnostics)
                observedErrors.Add(error.Id);

            CollectionAssert.AreEquivalent(errorCodes, observedErrors);
        }

        public static void TestCompilationThrows(string source, CompilationException exception, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationThrows(new[] { source }, exception, compilationPhases);

        public static void TestCompilationThrows(string[] sources, CompilationException exception, IEnumerable<IFullVisitor>? compilationPhases = null) {
            Compiler compiler = new();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);

            try {
                compiler.Compile(sources);
            } catch (CompilationException e) {
                Assert.AreEqual(exception.Message, e.Message, "Threw the wrong compilation exception!");
                return;
            } catch (Exception wrong) {
                Assert.Fail($"Expected to throw exception of type {typeof(CompilationException)} but got:\n{wrong}");
            }
            // Now we either succeeded compilation or have diagnostics.
            if (compiler.CompilationSucceeded) {
                Console.WriteLine("Compilation accidentally succeeded! Resulting code:");
                Console.WriteLine(compiler.CompiledDatapack.ToString());
                Console.WriteLine("\n\nResulting code in tree-form:");
                Console.WriteLine(compiler.CompiledDatapack.ToTreeString());
                Assert.Fail($"Expected to throw exception of type {typeof(CompilationException)} but did not throw any exception at all!");
            } else {
                Console.WriteLine("There were compilation errors:");
                foreach (var d in compiler.ErrorDiagnostics)
                    Console.WriteLine(d);
                Assert.Fail("There were compilation errors, but no exceptions!");
            }
        }
    }
}
