using Atrufulgium.FrontTick.Compiler.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    public static class Compiler {
        /// <summary>
        /// Returns a string containing the compiled datapack formatted as:
        /// <code>
        /// # (File &lt;filename.mcfunction&gt;)
        /// scoreboard player add blah blah blah
        /// 
        /// # (File &lt;filename2.mcfunction&gt;)
        /// say hi
        /// # More commands, more files, etc.
        /// </code>
        /// </summary>
        /// <param name="sources">
        /// A list of valid c# files that may reference eachother's contents.
        /// </param>
        /// <param name="references">
        /// A list of assembly references that the files in this compilation
        /// depend on. The <c>MCMirror</c> reference is automatically included.
        /// </param>
        public static string Compile(
            ICollection<string> sources, 
            ICollection<MetadataReference> references = null
        ) {
            // Automatically include the MCFunction reference.
            if (references == null)
                references = new List<MetadataReference>();
            var mcMirror = MetadataReference.CreateFromFile(typeof(MCMirror.MCFunctionAttribute).Assembly.Location);
            references.Add(mcMirror);

            var syntaxTrees = new List<SyntaxTree>(sources.Count);
            foreach(string source in sources) {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: "datapack",
                syntaxTrees: syntaxTrees,
                references: references
            );

            // TODO: One semantic model globally?
            List<SyntaxSemanticsPair> trees =
               (from syntaxTree in syntaxTrees
                select new SyntaxSemanticsPair(syntaxTree, compilation)
               ).ToList();

            // TODO: Lots of validation.

            var mcFunctions = new HashSet<MethodDeclarationSyntax>();
            foreach(var tree in trees) {
                var mcFunctionWalker = new MCFunctionWalker(tree.semantics);
                mcFunctionWalker.Visit(tree.syntax.GetCompilationUnitRoot());
                mcFunctions.UnionWith(mcFunctionWalker.mcFunctionMethods);
            }

            foreach(var func in mcFunctions) {
                Console.WriteLine(func.Identifier.ToString());
            }

            // TODO: Keep a stack of to-compile methods, starting with entry
            // points [MCFunction]s. When working through them and encountering
            // any uncompiled method, ditch all work and start compiling that.
            // Alternatively, do the above during validations.
            // In any case, don't be a dumbass and manage duplicates.

            // TODO: Far future when rewriting the tree: https://stackoverflow.com/a/12168782

            return "";
        }

        /// <inheritdoc cref="Compile(ICollection{string}, IEnumerable{MetadataReference})"/>
        /// <param name="source">
        /// The single source valid c# source file to compile. 
        /// </param>
        public static string Compile(
            string source,
            ICollection<MetadataReference> references = null
        ) => Compile(new[] { source }, references);
    }
}
