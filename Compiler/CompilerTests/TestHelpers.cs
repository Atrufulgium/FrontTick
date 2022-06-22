using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    internal static class TestHelpers {

        public static void TestCompilationSucceeds(string source, string output)
            => TestCompilationSucceeds(source, output, null);

        public static void TestCompilationSucceeds(string source, string output, IEnumerable<IFullVisitor>? compilationPhases)
            => TestCompilationSucceeds(new[] { source }, output, compilationPhases);

        public static void TestCompilationSucceeds(string[] sources, string output)
            => TestCompilationSucceeds(sources, output, null);

        public static void TestCompilationSucceeds(string[] sources, string output, IEnumerable<IFullVisitor>? compilationPhases) {
            Compiler compiler = new Compiler();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);
            compiler.Compile(sources);
            Assert.IsTrue(compiler.CompilationSucceeded);
            output = output.Replace("\r\n", "\n");
            Assert.AreEqual(output, compiler.CompiledDatapack.ToString());
        }

        public static void TestCompilationFails(string source, string errorCode)
            => TestCompilationFails(source, new[] { errorCode }, null);

        public static void TestCompilationFails(string source, string errorCode, IEnumerable<IFullVisitor>? compilationPhases)
            => TestCompilationFails(new[] { source }, new[] { errorCode }, compilationPhases);

        public static void TestCompilationFails(string[] sources, string errorCode)
            => TestCompilationFails(sources, new[] { errorCode }, null);

        public static void TestCompilationFails(string[] sources, string errorCode, IEnumerable<IFullVisitor>? compilationPhases)
            => TestCompilationFails(sources, new[] { errorCode }, compilationPhases);

        public static void TestCompilationFails(string source, string[] errorCodes)
            => TestCompilationFails(source, errorCodes, null);

        public static void TestCompilationFails(string source, string[] errorCodes, IEnumerable<IFullVisitor>? compilationPhases)
            => TestCompilationFails(new[] { source }, errorCodes, compilationPhases);

        public static void TestCompilationFails(string[] sources, string[] errorCodes)
            => TestCompilationFails(sources, errorCodes, null);

        public static void TestCompilationFails(string[] sources, string[] errorCodes, IEnumerable<IFullVisitor>? compilationPhases) {
            Compiler compiler = new Compiler();
            if (compilationPhases != null)
                compiler.SetCompilationPhases(compilationPhases);
            compiler.Compile(sources);
            Assert.IsFalse(compiler.CompilationSucceeded);

            List<string> observedErrors = new();
            foreach (var error in compiler.ErrorDiagnostics)
                observedErrors.Add(error.Id);

            CollectionAssert.AreEquivalent(errorCodes, observedErrors);
        }

    }
}
