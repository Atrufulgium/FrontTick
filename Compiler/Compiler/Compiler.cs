using Atrufulgium.FrontTick.Compiler.Walkers;
using MCMirror;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    public class Compiler {

        public Datapack CompiledDatapack => new Datapack(finishedCompilation);
        public bool CompilationSucceeded => ErrorDiagnostics.Count == 0;
        public ReadOnlyCollection<Diagnostic> ErrorDiagnostics { get; private set; }
        public ReadOnlyCollection<Diagnostic> WarningDiagnostics { get; private set; }
        List<Diagnostic> errorDiagnostics;
        List<Diagnostic> warningDiagnostics;

        /// <summary>
        /// All trees we are considering, sementically meaningful.
        /// </summary>
        List<SyntaxSemanticsPair> trees;
        /// <summary>
        /// All method entry points we have yet to compile. Items higher in the
        /// stack should not depend on items lower in the stack.
        /// </summary>
        StackWithoutDuplicates<EntryPoint> entryPoints;
        /// <summary>
        /// All work that is done so far.
        /// </summary>
        List<DatapackFile> finishedCompilation;

        string manespace;

        /// <summary>
        /// A list of types whose assemblies to automatically include in any
        /// compilation. The actual listed types don't matter, just their
        /// namespace.
        /// </summary>
        Type[] autoIncludeAssemblyTypes = new[] {
            typeof(MCMirror.MCFunctionAttribute),
            typeof(System.Object)
        };

        public Compiler() {
            errorDiagnostics = new();
            warningDiagnostics = new();
            ErrorDiagnostics = new(errorDiagnostics);
            WarningDiagnostics = new(warningDiagnostics);
            entryPoints = new();
            finishedCompilation = new();
        }

        /// <summary>
        /// <para>
        /// Compiles a bunch of c# code into a datapack. Returns whether
        /// compilation succeeds, which can also be read from the property
        /// <see cref="CompilationSucceeded"/>.
        /// </para>
        /// <para>
        /// The resulting datapack can be read from the property
        /// <see cref="CompiledDatapack"/>.
        /// </para>
        /// </summary>
        /// <param name="sources">
        /// A list of valid c# files that may reference eachother's contents.
        /// </param>
        /// <param name="manespace">
        /// The datapack namespace to put all functions into.
        /// </param>
        /// <param name="references">
        /// A list of assembly references that the files in this compilation
        /// depend on. The <c>System</c> and <c>MCMirror</c> references are
        /// automatically included.
        /// </param>
        public bool Compile(
            ICollection<string> sources,
            string manespace = "compiled",
            ICollection<MetadataReference> references = null
        ) {
            this.manespace = manespace;
            // Automatically include the basic references we need.
            if (references == null)
                references = new List<MetadataReference>();

            // There's a few assemblies that need to be manually added that
            // won't work with the typeof hack.
            foreach (var reference in GetHardMetadataReferences())
                references.Add(reference);

            // The long list of possible types' assemblies
            foreach (var assemblyType in autoIncludeAssemblyTypes) {
                var assembly = MetadataReference.CreateFromFile(assemblyType.Assembly.Location);
                references.Add(assembly);
            }

            var syntaxTrees = new List<SyntaxTree>(sources.Count);
            foreach(string source in sources) {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: "compiled",
                syntaxTrees: syntaxTrees,
                references: references
            );

            trees = (from syntaxTree in syntaxTrees
                select new SyntaxSemanticsPair(syntaxTree, compilation)
                ).ToList();

            // Check if vanilla c# went perfectly fine.
            foreach(var tree in trees)
                AppendDiagnostics(tree.semantics.GetDiagnostics());

            if (!CompilationSucceeded)
                return false;

            // Find the basic entrypoints of compilation. We will later find
            // more stuff that we need, but that is ultimately referenced from
            // these methods.

            foreach(var tree in trees) {
                var mcFunctionWalker = new MCFunctionWalker(tree.semantics);
                mcFunctionWalker.Visit(tree.syntax.GetCompilationUnitRoot());
                AppendDiagnostics(mcFunctionWalker.customDiagnostics);

                entryPoints.AddRange(mcFunctionWalker.foundMethods);
            }

            if (!CompilationSucceeded)
                return false;

            // Compile every individual method.

            while (entryPoints.Count > 0) {
                if (CompileMethod(entryPoints.Peek()))
                    entryPoints.Pop();

                if (!CompilationSucceeded)
                    return false;
            }

            // TODO: Keep a stack of to-compile methods, starting with entry
            // points [MCFunction]s. When working through them and encountering
            // any uncompiled method, ditch all work and start compiling that.
            // Alternatively, do the above during validations.
            // In any case, don't be a dumbass and manage duplicates.

            // TODO: Far future when rewriting the tree: https://stackoverflow.com/a/12168782

            return true;
        }

        /// <inheritdoc cref="Compile(ICollection{string}, string, ICollection{MetadataReference})"/>
        /// <param name="source">
        /// The single source valid c# source file to compile. 
        /// </param>
        public bool Compile(
            string source,
            string manespace = "compiled",
            ICollection<MetadataReference> references = null
        ) => Compile(new[] { source }, manespace, references);

        /// <summary>
        /// Compiles a single method. Returns whether it succeeded (which is
        /// only allowed if it does not add further entry points to
        /// <see cref="entryPoints"/>).
        /// </summary>
        bool CompileMethod(EntryPoint entry) {
            string path = GetMCFunctionName(entry);
            DatapackFile finishedFile = new DatapackFile(path, manespace);

            // The important stuff

            finishedCompilation.Add(finishedFile);
            return true;
        }

        internal string GetMCFunctionName(EntryPoint entry) {
            string path;
            var semantics = entry.tree.semantics;
            if (entry.method.TryGetSemanticAttributeOfType(typeof(MCFunctionAttribute), entry.tree.semantics, out var attrib)) {
                if (attrib.ConstructorArguments.Length == 0)
                    path = semantics.GetFullyQualifiedMethodName(entry.method);
                else
                    path = (string)attrib.ConstructorArguments[0].Value;
            } else {
                path = "internal/" + semantics.GetTypeInfo(entry.method).ToString();
            }
            return DatapackFile.NormalizeFunctionName(path);
        }

        /// <summary>
        /// Sorts and appends all diagnostics in the IEnumerable into the
        /// <see cref="WarningDiagnostics"/> and <see cref="ErrorDiagnostics"/>
        /// properties (discarding non-warning non-error information).
        /// </summary>
        void AppendDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            foreach(var diagnostic in diagnostics) {
                if (diagnostic.Severity == DiagnosticSeverity.Warning)
                    warningDiagnostics.Add(diagnostic);
                else if (diagnostic.Severity == DiagnosticSeverity.Error)
                    errorDiagnostics.Add(diagnostic);
            }
        }

        /// <summary>
        /// A few assemblies are annoying and need to manually be added.
        /// </summary>
        static List<MetadataReference> GetHardMetadataReferences() {
            // See https://stackoverflow.com/a/39049422
            var assemblyLocation = typeof(object).Assembly.Location;
            var coreDir = System.IO.Directory.GetParent(assemblyLocation);

            List<MetadataReference> returnList = new();
            foreach(var a in new[] { /*"mscorlib",*/ "netstandard", "System.Runtime" }) {
                returnList.Add(MetadataReference.CreateFromFile(
                    $"{coreDir.FullName}{System.IO.Path.DirectorySeparatorChar}{a}.dll"
                    )
                );
            }
            return returnList;
        }
    }
}
