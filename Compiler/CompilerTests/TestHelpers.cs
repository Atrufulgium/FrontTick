using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    /// <summary>
    /// A class for easier compiler testing.
    /// No need to include the MCMirror code anywhere explicitely, it is
    /// automatically included from its project in this solution.
    /// </summary>
    internal static class TestHelpers {

        /// <summary>
        /// <para>
        /// Whether the compiled code gives a specific output. This output is
        /// very dependent on the compiler itself, and it would be much better
        /// to use <see cref="TestCompilationSucceedsRaw(string, string, IEnumerable{IFullVisitor}?)"/>
        /// instead.
        /// </para>
        /// <para>
        /// To make it clear that a test result depends on the compiler output,
        /// please append <tt>Raw</tt> to any test using this method.
        /// </para>
        /// </summary>
        public static void TestCompilationSucceedsRaw(string source, string output, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationSucceedsRaw(new[] { source }, output, compilationPhases);

        /// <inheritdoc cref="TestCompilationSucceedsRaw(string, string, IEnumerable{IFullVisitor}?)"/>
        public static void TestCompilationSucceedsRaw(string[] sources, string output, IEnumerable<IFullVisitor>? compilationPhases = null) {
            string actual = CompileToString(sources, compilationPhases, out Compiler compiler, post: new NamePostProcessors.Identity());
            // Normalize the string to proper newlines, removed whitespace, and
            // a single newline before/after to make the test output readable.
            output = output.Replace("\r\n", "\n").Trim();
            output = $"\n{output}\n";
            try {
                Assert.AreEqual(output, actual);
            } catch (AssertFailedException e) {
                Console.WriteLine("Resulting code in tree-form:");
                Console.WriteLine(compiler.CompiledDatapack.ToTreeString());
                throw e;
            }
        }

        public static void TestCompilationSucceedsTheSame(
            string source1,
            string source2,
            IEnumerable<IFullVisitor>? compilationPhases1 = null,
            IEnumerable<IFullVisitor>? compilationPhases2 = null
        ) => TestCompilationSucceedsTheSame(new[] { source1 }, new[] { source2 }, compilationPhases1, compilationPhases2);

        public static void TestCompilationSucceedsTheSame(
            string source1,
            string[] sources2,
            IEnumerable<IFullVisitor>? compilationPhases1 = null,
            IEnumerable<IFullVisitor>? compilationPhases2 = null
        ) => TestCompilationSucceedsTheSame(new[] { source1 }, sources2, compilationPhases1, compilationPhases2);

        public static void TestCompilationSucceedsTheSame(
            string[] sources1,
            string source2,
            IEnumerable<IFullVisitor>? compilationPhases1 = null,
            IEnumerable<IFullVisitor>? compilationPhases2 = null
        ) => TestCompilationSucceedsTheSame(sources1, new[] { source2 }, compilationPhases1, compilationPhases2);

        public static void TestCompilationSucceedsTheSame(
            string[] sources1,
            string[] sources2,
            IEnumerable<IFullVisitor>? compilationPhases1 = null,
            IEnumerable<IFullVisitor>? compilationPhases2 = null
        ) {
            string out1 = CompileToString(sources1, compilationPhases1, out Compiler compiler1, "There were compilation errors in sources1:", new NamePostProcessors.ConvenientTests());
            string out2 = CompileToString(sources2, compilationPhases2, out Compiler compiler2, "There were compilation errors in sources2:", new NamePostProcessors.ConvenientTests());
            
            try {
                if (out1.Trim() == "" || out2.Trim() == "")
                    Assert.Fail("Empty compiled results will fail `TestCompilationSucceedsTheSame` tests.");
                Assert.AreEqual(out1, out2);
            } catch (AssertFailedException e) {
                Console.WriteLine("First code in tree-form:");
                Console.WriteLine(compiler1.CompiledDatapack.ToTreeString());
                Console.WriteLine("\nSecond code in tree-form:");
                Console.WriteLine(compiler2.CompiledDatapack.ToTreeString());
                throw e;
            }
        }

        private static string CompileToString(
            string[] sources,
            IEnumerable<IFullVisitor>? compilationPhases,
            out Compiler compiler,
            string failTitle = "There were compilation errors:",
            INameManagerPostProcessor? post = null
        ) {
            compilationPhases ??= CompilationPhases.BasicCompilationPhases;
            compilationPhases = compilationPhases.Prepend(new MakeCompilerTestingEasierRewriter());

            compiler = new(nameManagerPostProcessor: post);
            compiler.SetCompilationPhases(compilationPhases);
            compiler.Compile(sources.Concat(GetMCMirrorCode()));
            try {
                Assert.IsTrue(compiler.CompilationSucceeded);
            }
            catch (AssertFailedException e) {
                Console.WriteLine(failTitle);
                foreach (var d in compiler.ErrorDiagnostics)
                    Console.WriteLine(d);
                throw e;
            }
            // Normalize the string to proper newlines, removed whitespace, and
            // a single newline before/after to make the test output readable.
            var output = compiler.CompiledDatapack.ToString(skipInternal: true, skipMCMirror: true).Replace("\r\n", "\n");
            output = $"\n{output.Trim()}\n";
            return output;
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
            compiler.Compile(sources.Concat(GetMCMirrorCode()));
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

            try {
                CollectionAssert.AreEquivalent(errorCodes, observedErrors);
            } catch (AssertFailedException e) {
                Console.WriteLine("There were different compilation errors than expected:");
                foreach (var d in compiler.ErrorDiagnostics)
                    Console.WriteLine(d);
                throw e;
            }
        }

        public static void TestCompilationThrows(string source, CompilationException exception, IEnumerable<IFullVisitor>? compilationPhases = null)
            => TestCompilationThrows(new[] { source }, exception, compilationPhases);

        public static void TestCompilationThrows(string[] sources, CompilationException exception, IEnumerable<IFullVisitor>? compilationPhases = null) {
            Compiler compiler = new();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);

            try {
                compiler.Compile(sources.Concat(GetMCMirrorCode()));
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

        static IEnumerable<string>? mcMirror = null;
        private static IEnumerable<string> GetMCMirrorCode() {
            if (mcMirror != null)
                return mcMirror;

            string path = Environment.CurrentDirectory;
            string target = $"FrontTick{Path.DirectorySeparatorChar}Compiler";
            while (!path.EndsWith(target))
                path = Directory.GetParent(path)!.FullName;
            path += $"{Path.DirectorySeparatorChar}MCMirror";
            mcMirror = from codepaths in FolderToContainingCode.GetCode(path) select codepaths.code;
            return mcMirror;
        }
    }
}
