using Atrufulgium.FrontTick.Compiler.Collections;
using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// A compiler for turning a bunch of c# files into a Minecraft datapack. 
    /// </para>
    /// <para>
    /// A typical compilation process looks as follows:
    /// <list type="number">
    /// <item><description>
    /// Create a new <see cref="Compiler"/> instance, optionally specifying
    /// which minecraft-datapack namespace it will live in, and what other
    /// assemblies to reference.
    /// </description></item>
    /// <item><description>
    /// If you want any advanced functionality, set what compilation phases
    /// you want via <see cref="SetCompilationPhases(IEnumerable{IFullVisitor})"/>.
    /// If you don't, the output will be basic and unoptimised. There are various
    /// default presets, found in the <see cref="CompilationPhases"/> class.
    /// </description></item>
    /// <item><description>
    /// Check whether compilation succeeded with <see cref="CompilationSucceeded"/>
    /// (or its opposite, <see cref="CompilationFailed"/>). If it succeeded,
    /// you can use the resulting <see cref="CompiledDatapack"/> for instance
    /// via <see cref="Datapack.WriteToFilesystem(string)"/>. Otherwise, check
    /// the problems with <see cref="ErrorDiagnostics"/>. In either case, the
    /// warnings <see cref="WarningDiagnostics"/> may be of interest.
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Whether succeeding or failing, each instance is valid for only one
    /// compilation.
    /// </para>
    /// </summary>
    public class Compiler {

        public Datapack CompiledDatapack => new(finishedCompilation, nameManager) {
            testFunctions = appliedWalkers.Get<ProcessedToDatapackWalker>().testFunctions
        };

        public bool CompilationSucceeded => ErrorDiagnostics.Count == 0;
        public bool CompilationFailed => !CompilationSucceeded;

        public ReadOnlyCollection<Diagnostic> ErrorDiagnostics { get; private set; }
        public ReadOnlyCollection<Diagnostic> WarningDiagnostics { get; private set; }
        List<Diagnostic> errorDiagnostics = new();
        List<Diagnostic> warningDiagnostics = new();

        bool hasCompiled = false;

        /// <summary>
        /// All method trees we are compiling. These contain the
        /// [MCFunction]-tagged methods and their dependencies we care about.
        /// </summary>
        // TODO: Don't do this, instead start with a step that prunes all unused methods.
        // After that, *all* methods are entrypoints, and the immutability of
        // the tree is no problem any longer.
        internal readonly HashSet<SemanticModel> roots = new();
        /// <summary>
        /// All work that is done so far.
        /// </summary>
        internal readonly List<DatapackFile> finishedCompilation = new();
        /// <summary>
        /// All applied transformations on the syntax tree so far.
        /// </summary>
        internal readonly SetByType appliedWalkers = new();
        /// <summary>
        /// Handling all method names and easy (local) name conversions.
        /// </summary>
        internal readonly NameManager nameManager;
        private CSharpCompilation compilation;
        /// <summary>
        /// All transformations that we will have applied to the syntax tree.
        /// </summary>
        private IFullVisitor[] compilationPhases;
        /// <summary>
        /// All references this compilation will use.
        /// </summary>
        private readonly HashSet<MetadataReference> references;

        /// <summary>
        /// Create a new compiler instance. This instance gets compilation
        /// phases set to the minimum bare-bones to get a working result. For
        /// better results (including optimisation), also use
        /// <see cref="SetCompilationPhases(IEnumerable{IFullVisitor})"/>.
        /// </summary>
        /// <param name="manespace">
        /// The datapack namespace to put all functions into.
        /// </param>
        /// <param name="references">
        /// A list of assembly references that the files in this compilation
        /// depend on. The <c>System</c> and <c>MCMirror</c> references are
        /// automatically included.
        /// </param>
        public Compiler(
            string manespace = "compiled",
            ICollection<MetadataReference> references = null
        ) {
            ErrorDiagnostics = new(errorDiagnostics);
            WarningDiagnostics = new(warningDiagnostics);

            nameManager = new(manespace);
            this.references = ReferenceManager.GetReferences(references);

            SetCompilationPhases(CompilationPhases.BasicCompilationPhases);
        }

        /// <summary>
        /// Sets the various compilation phases for this compiler.
        /// </summary>
        /// <param name="compilationPhases">
        /// All the phases that make up this compiler. This include basic
        /// things like doing the "turning it into a datapack", but also the
        /// optimisations and such.
        /// </param>
        public void SetCompilationPhases(IEnumerable<IFullVisitor> compilationPhases) {
            this.compilationPhases = ApplyDependencies(compilationPhases).ToArray();
        }

        /// <summary>
        /// <para>
        /// This method goes through each visitor (which MUST inherit either
        /// <see cref="AbstractFullRewriter"/> or <see cref="AbstractFullWalker"/>
        /// and variants!) and adds any missing dependencies to this compiler
        /// process.
        /// </para>
        /// <para>
        /// Any phase that is required earlier as dependency is moved.
        /// </para>
        /// </summary>
        IEnumerable<IFullVisitor> ApplyDependencies(IEnumerable<IFullVisitor> compilationPhases) {
            var phasesList = (from p in compilationPhases select p.GetType()).ToList();
            var toHandle = new Queue<Type>(phasesList);
            while(toHandle.Count > 0) {
                var visitor = toHandle.Dequeue();
                var baseType = visitor.BaseType;
                foreach(Type dependency in baseType.GenericTypeArguments) {
                    // We care mostly about type, so need to manually walk the list.
                    // Good 'ol O(ew). Luckily these lists will never be too large
                    // so I don't care about more sophisticated methods.
                    // What are topologically sorted graphs etc etc
                    // TODO: If multiple, remove duplicates?
                    int visitorIndex = -1;
                    bool foundDependency = false;
                    int dependencyIndex = -1;
                    for (int i = 0; i < phasesList.Count; i++) {
                        if (phasesList[i] == dependency) {
                            dependencyIndex = i;
                            foundDependency = true;
                        }
                        if (phasesList[i] == visitor)
                            visitorIndex = i;
                    }
                    if (dependencyIndex > visitorIndex) {
                        // This dependency is too late in the list and needs
                        // to be moved ahead.
                        // Note that this is viral and this dependency's
                        // dependencies may need to be moved ahead as well.
                        phasesList.RemoveAt(dependencyIndex);
                        foundDependency = false;
                        foreach (Type dependencyDependency in dependency.GenericTypeArguments)
                            toHandle.Enqueue(dependencyDependency);
                    }
                    if (!foundDependency) {
                        toHandle.Enqueue(dependency);
                        phasesList.Insert(visitorIndex, dependency);
                    }
                }
            }
            return from p in phasesList select (IFullVisitor)Activator.CreateInstance(p);
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
        public bool Compile(
            IEnumerable<string> sources
        ) {
            if (hasCompiled)
                throw new InvalidOperationException("This instance has already compiled once. To recompile, use a new Compiler instance.");
            hasCompiled = true;

            var syntaxTrees = new List<SyntaxTree>();
            foreach(string source in sources) {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
            }

            compilation = CSharpCompilation.Create(
                assemblyName: "compiled",
                syntaxTrees: syntaxTrees,
                references: references
            );

            var models = from syntaxTree in syntaxTrees
                        select compilation.GetSemanticModel(syntaxTree);

            // Check if vanilla c# went perfectly fine, and in the meantime,
            // add our entry points -- the models with their roots.
            roots.Clear();
            foreach (var model in models) {
                AppendDiagnostics(model.GetDiagnostics());
                roots.Add(model);
            }
            if (CompilationFailed)
                return false;

            foreach(var phase in compilationPhases) {
                phase.SetCompiler(this);
                phase.FullVisit();
                AppendDiagnostics(phase.CustomDiagnostics);
                if (CompilationFailed)
                    return false;
                appliedWalkers.AddByMostDerived(phase);
            }

            // Now add the setup file with constants and such
            DatapackFile setupFile = new(nameManager.SetupFileName);
            setupFile.code.Add("scoreboard objectives add _ dummy");
            setupFile.code.Add("scoreboard players set #GOTOFLAG _ 0");
            setupFile.code.Add("scoreboard players set #FLAGFOUND _ 0");
            // Maintain this setting across reloads.
            setupFile.code.Add("execute unless score #FAILSONLY _ matches 0.. run scoreboard players set #FAILSONLY _ 0");
            setupFile.code.Add("scoreboard players set #TESTSUCCESSES _ 0");
            setupFile.code.Add("scoreboard players set #TESTFAILURES _ 0");
            setupFile.code.Add("scoreboard players set #TESTSSKIPPED _ 0");
            // Don't need to set #RET as it can only be used after a function
            // returns -- it is always set when read.
            foreach (int i in nameManager.Constants.ToList())
                setupFile.code.Add($"scoreboard players set {nameManager.GetConstName(i)} _ {i}");
            // On my machine, I can get ~125k (fast) commands / tick.
            // Then 2 seconds worth of commands seems like a reasonable limit,
            // especially for just 1 tick.
            setupFile.code.Add("gamerule maxCommandChainLength 5000000");
            finishedCompilation.Add(setupFile);

            // Also the test post processing file.
            DatapackFile postTestFile = new(nameManager.TestPostProcessName);
            postTestFile.code.Add("tellraw @a [{\"text\":\"Testing complete.\",\"color\":\"white\"},{\"text\":\"\\n  Successes: \",\"color\":\"green\"},{\"score\":{\"name\":\"#TESTSUCCESSES\",\"objective\":\"_\"},\"bold\":true,\"color\":\"dark_green\"},{\"text\":\"\\n  Failures: \",\"color\":\"red\"},{\"score\":{\"name\":\"#TESTFAILURES\",\"objective\":\"_\"},\"bold\":true,\"color\":\"dark_red\"}]");
            postTestFile.code.Add("execute unless score #TESTSSKIPPED _ matches 0 run tellraw @a [{\"text\":\"  Skipped: \",\"color\":\"red\"},{\"score\":{\"name\":\"#TESTSSKIPPED\",\"objective\":\"_\"},\"bold\":true,\"color\":\"dark_red\"}]");
            postTestFile.code.Add("execute if score #FAILSONLY _ matches 0 run tellraw @a {\"text\":\"(To hide subsequent successes, click here.)\",\"color\":\"dark_gray\",\"clickEvent\":{\"action\":\"run_command\",\"value\":\"/scoreboard players set #FAILSONLY _ 1\"},\"hoverEvent\":{\"action\":\"show_text\",\"contents\":[\"Turn successes display off.\"]}}");
            postTestFile.code.Add("execute unless score #FAILSONLY _ matches 0 run tellraw @a {\"text\":\"(To show subsequent successes, click here.)\",\"color\":\"dark_gray\",\"clickEvent\":{\"action\":\"run_command\",\"value\":\"/scoreboard players set #FAILSONLY _ 0\"},\"hoverEvent\":{\"action\":\"show_text\",\"contents\":[\"Turn successes display on.\"]}}");
            postTestFile.code.Add("scoreboard players set #TESTSUCCESSES _ 0");
            postTestFile.code.Add("scoreboard players set #TESTFAILURES _ 0");
            postTestFile.code.Add("scoreboard players set #TESTSSKIPPED _ 0");
            finishedCompilation.Add(postTestFile);

            return true;
        }

        /// <summary>
        /// Replaces the old code (with attached semantic model) with new code
        /// (as generated by <see cref="AbstractFullRewriter"/>).
        /// </summary>
        public void ReplaceTree(SemanticModel oldModel, SyntaxNode newRoot) {
            compilation = compilation.ReplaceSyntaxTree(oldModel.SyntaxTree, newRoot.SyntaxTree);
            var newModel = compilation.GetSemanticModel(newRoot.SyntaxTree);
            roots.Remove(oldModel);
            roots.Add(newModel);
        }

        /// <inheritdoc cref="Compile(ICollection{string}, string, ICollection{MetadataReference})"/>
        /// <param name="source">
        /// The single source valid c# source file to compile. 
        /// </param>
        public bool Compile(
            string source
        ) => Compile(new[] { source });

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
    }
}
