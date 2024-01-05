using Atrufulgium.FrontTick.Compiler.Collections;
using Atrufulgium.FrontTick.Compiler.Datapack;
using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler
{
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
    /// via <see cref="FullDatapack.WriteToFilesystem(string)"/>. Otherwise, check
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

        public FullDatapack CompiledDatapack => new(
            finishedCompilation,
            appliedWalkers.Get<LoadTickWalker>().GeneratedFiles
        );

        public bool CompilationSucceeded => ErrorDiagnostics.Count == 0;
        public bool CompilationFailed => !CompilationSucceeded;

        public ReadOnlyCollection<Diagnostic> ErrorDiagnostics { get; private set; }
        public ReadOnlyCollection<Diagnostic> WarningDiagnostics { get; private set; }
        readonly List<Diagnostic> errorDiagnostics = new();
        readonly List<Diagnostic> warningDiagnostics = new();

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
        internal readonly List<IDatapackFile> finishedCompilation = new();
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
        /// Useful for debugging. Prints all current sources back to back.
        /// When an error diagonstic is reached, you can browse these for a
        /// pretty printed experience. (Line/col numbers won't match though.0
        /// </summary>
        private string PrettyPrintAllSources => string.Join("\r\n", from r in roots select $"// {r.SyntaxTree.FilePath}:\r\n{r.SyntaxTree.GetRoot().NormalizeWhitespace()}\r\n");

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
        /// <param name="nameManagerPostProcessor">
        /// How to post-process all function and variable names. If null, uses
        /// <see cref="NamePostProcessors.Identity"/>.
        /// </param>
        public Compiler(
            string manespace = "compiled",
            ICollection<MetadataReference> references = null,
            INameManagerPostProcessor nameManagerPostProcessor = null
        ) {
            ErrorDiagnostics = new(errorDiagnostics);
            WarningDiagnostics = new(warningDiagnostics);

            nameManagerPostProcessor ??= new NamePostProcessors.Identity();

            nameManager = new(manespace, nameManagerPostProcessor);
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
            var phasesDepth = phasesList.ToDictionary(p => p, p => 0);
            var toHandle = new Queue<Type>(phasesList);
            while(toHandle.Count > 0) {
                var visitor = toHandle.Dequeue();
                var baseType = visitor.BaseType;
                int currentDepth = phasesDepth[visitor];
                foreach(Type dependency in baseType.GenericTypeArguments) {
                    // Prevent headache later
                    if (!phasesDepth.ContainsKey(dependency))
                        phasesDepth[dependency] = -1;

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

                    // The depth needs to be updated in two cases:
                    // - It doesn't exist yet
                    // - It gets moved ahead
                    // In both cases, the last branch happens and the dependency
                    // inherits the current one's depth + 1.
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
                        phasesDepth[dependency] = currentDepth + 1;
                        toHandle.Enqueue(dependency);
                        phasesList.Insert(visitorIndex, dependency);
                    }
                }
            }
            return phasesList.Select(p => {
                var v = (IFullVisitor)Activator.CreateInstance(p);
                v.DependencyDepth = phasesDepth[p];
                return v;
            });
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
        /// Each is tagged with a filename.
        /// </param>
        public bool Compile(
            IEnumerable<(string code, string path)> sources
        ) {
            if (hasCompiled)
                throw new InvalidOperationException("This instance has already compiled once. To recompile, use a new Compiler instance.");
            hasCompiled = true;

            var syntaxTrees = new List<SyntaxTree>();
            foreach(var (code, path) in sources) {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: path));
            }

            compilation = CSharpCompilation.Create(
                assemblyName: "compiled",
                syntaxTrees: syntaxTrees,
                references: references,
                options: new(OutputKind.DynamicallyLinkedLibrary) // to not have CS5001
            );

            var models = from syntaxTree in syntaxTrees
                        select compilation.GetSemanticModel(syntaxTree);

            // Check if vanilla c# went perfectly fine, and in the meantime,
            // add our entry points -- the models with their roots.
            roots.Clear();
            foreach (var model in models) {
                var diagnostics = model.GetDiagnostics();
                AppendDiagnostics(diagnostics);
                roots.Add(model);
            }
            if (CompilationFailed)
                return false;

            int phaseID = 1;
            // All errors allowed *to be introduced by the compiler*.
            // User code must still satisfy vanilla c#.
            string[] allowedErrors = new[] { 
                // "No such label 'label' within the scope of the goto statement"
                // We introduce this in GotoFlagifyRewriter and manage this in ProcessedToDatapackWalker.
                "CS0159",
                // "An attribute argument must be a constant expression, [etc]"
                // We introduce this in general as rewrites also touch attributes' expressions.
                // This is just a slightly more general thing to keep in mind.
                "CS0182",
            };
            bool incorrectTreeAllowed = false;
            StringBuilder errors = new();
            int prevDepth = -1;
            foreach(var phase in compilationPhases) {
                // I should really extract logging to someplace else.
                int depth = phase.DependencyDepth;
                string indent = "";
                for (int i = 0; i < depth - 1; i++)
                    indent += "│ ";
                if (depth > 0)
                    if (prevDepth >= depth)
                        indent += "├─";
                    else
                        indent += "┌─";
                prevDepth = depth;

                string rhs = $"{phase.GetType().Name}";
                string lhs = $"[{DateTime.Now.TimeOfDay}] ";
                if (rhs.Contains("Category"))
                    lhs += "(Category)";
                else
                    lhs += $"Phase {phaseID++,-4}";
                string colorless = $" - {indent}";
                var consoleColor = ConsoleColor.Gray;
                if (rhs.Contains("Category"))
                    consoleColor = ConsoleColor.Yellow;
                else if (rhs.Contains("ProcessedToDatapackWalker"))
                    consoleColor = ConsoleColor.Cyan;
                else if (rhs.Contains("Walker"))
                    consoleColor = ConsoleColor.DarkGray;
                Console.ForegroundColor = consoleColor;
                Console.Write(lhs);
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(colorless);
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(rhs);
                Console.ResetColor();

                // ACTUALLY do the thing!
                phase.SetCompiler(this);
                phase.FullVisit();
                AppendDiagnostics(phase.CustomDiagnostics);
                if (CompilationFailed)
                    return false;
                appliedWalkers.AddByMostDerived(phase);

                incorrectTreeAllowed |= phase is ReturnRewriter;
                errors.Clear();
                if (!incorrectTreeAllowed)
                    foreach (var d in compilation.GetDiagnostics())
                        if (d.Severity == DiagnosticSeverity.Error && !allowedErrors.Contains(d.Id))
                            errors.AppendLine(CSharpDiagnosticFormatter.Instance.Format(d));
                if (errors.Length > 0)
                    throw new CompilationException($"Error(s) after phase {phase.GetType().Name} (#{--phaseID}/{compilationPhases.Length}):\n{errors}");
            }

            // Now add the setup file with constants and such
            MCFunctionFile setupFile = new(nameManager.SetupFileName);
            setupFile.code.Add("scoreboard objectives add _ dummy");
            setupFile.code.Add("scoreboard players set #GOTOFLAG _ 0");
            // Maintain this setting across reloads.
            setupFile.code.Add("execute unless score #FAILSONLY _ matches 0.. run scoreboard players set #FAILSONLY _ 0");
            setupFile.code.Add("scoreboard players set #TESTSUCCESSES _ 0");
            setupFile.code.Add("scoreboard players set #TESTFAILURES _ 0");
            setupFile.code.Add("scoreboard players set #TESTSSKIPPED _ 0");
            // Don't need to set #RET as it can only be used after a function
            // returns -- it is always set when read.
            foreach (var (num, name) in nameManager.GetAllConstantNames())
                setupFile.code.Add($"scoreboard players set {name} _ {num}");
            // Add a call to all >1-tick function tags
            foreach (var (tickrate, index) in appliedWalkers.Get<LoadTickWalker>().ActiveLongerTickPairs) {
                // +1 to ensure none are run on the load frame.
                // (Also because a schedule of 0 throws an error and I'm too lazy to differentiate cases)
                setupFile.code.Add($"schedule function #{nameManager.manespace}:{LoadTickWalker.GetTickName(tickrate, index)} {index + 1}t");
            }
            // On my machine, I can get ~125k (fast) commands / tick.
            // Then 2 seconds worth of commands seems like a reasonable limit,
            // especially for just 1 tick.
            setupFile.code.Add("gamerule maxCommandChainLength 5000000");
            finishedCompilation.Add(setupFile);

            // Also (as mentioned in LoadTickWalker.cs), add all "reschedule
            // own tick" methods that already exist in the tag.
            foreach(var (tickrate, index) in appliedWalkers.Get<LoadTickWalker>().ActiveLongerTickPairs) {
                // Same name as over there, same mcfunctionname violation as over there
                string functionName = $"{nameManager.manespace}:{LoadTickWalker.GetTickName(tickrate, index)}";
                MCFunctionFile rescheduleFile = new(new(functionName));
                // Note that scheduled functions *persist through reloads*.
                // Fortunately, the default schedule function replaces, so it
                // doesn't matter.
                rescheduleFile.code.Add($"schedule function #{functionName} {tickrate}t");
                finishedCompilation.Add(rescheduleFile);
            }

            return true;
        }

        /// <inheritdoc cref="Compile(IEnumerable{(string, string)})"/>
        /// <param name="sources">
        /// A list of valid c# files that may reference eachother's contents.
        /// </param>
        public bool Compile(
            IEnumerable<string> sources
        ) => Compile(from s in sources select (s, "<No Path>"));

        /// <inheritdoc cref="Compile(ICollection{string}, string, ICollection{MetadataReference})"/>
        /// <param name="source">
        /// The single source valid c# source file to compile. 
        /// </param>
        public bool Compile(
            string source
        ) => Compile(new[] { source });

        /// <summary>
        /// Replaces the old code (with attached semantic model) with new code
        /// (as generated by <see cref="AbstractFullRewriter"/>).
        /// </summary>
        public void ReplaceTree(SemanticModel oldModel, SyntaxNode newRoot) {
            var correctPathTree = newRoot.SyntaxTree.WithFilePath(oldModel.SyntaxTree.FilePath);
            compilation = compilation.ReplaceSyntaxTree(oldModel.SyntaxTree, correctPathTree);
            var newModel = compilation.GetSemanticModel(correctPathTree);
            roots.Remove(oldModel);
            roots.Add(newModel);
        }

        /// <summary>
        /// An unsafe method that manually adds a datapack file to the
        /// finalized list. Modifications after this to this file are not
        /// supported.
        /// </summary>
        public void ManuallyFinalizeDatapackFile(IDatapackFile file) {
            finishedCompilation.Add(file);
        }

        readonly HashSet<string> FilteredWarnings = new() {
            "CS0660", // Type defines operator == or operator != but does not override Object.Equals(object o)
            "CS0661", // Type defines operator == or operator != but does not override Object.GetHashCode()
        };

        /// <summary>
        /// <para>
        /// Sorts and appends all diagnostics in the IEnumerable into the
        /// <see cref="WarningDiagnostics"/> and <see cref="ErrorDiagnostics"/>
        /// properties (discarding non-warning non-error information).
        /// </para>
        /// </summary>
        void AppendDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            foreach(var diagnostic in diagnostics) {
                if (diagnostic.Severity == DiagnosticSeverity.Warning && !FilteredWarnings.Contains(diagnostic.Id))
                    warningDiagnostics.Add(diagnostic);
                else if (diagnostic.Severity == DiagnosticSeverity.Error)
                    errorDiagnostics.Add(diagnostic);
            }
        }
    }
}
