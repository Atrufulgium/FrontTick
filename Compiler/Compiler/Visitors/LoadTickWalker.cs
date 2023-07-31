using Atrufulgium.FrontTick.Compiler.Datapack;
using MCMirror;
using MCMirror.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Walks the tree to collect all methods with <see cref="LoadAttribute"/>,
    /// <see cref="TrueLoadAttribute"/>, and <see cref="TickAttribute"/>.
    /// </summary>
    public class LoadTickWalker : AbstractFullWalker {

        private FunctionTag loadTag;
        private FunctionTag tickTag;
        private FunctionTag trueLoadTag;

        readonly Dictionary<(int tickrate, int index), FunctionTag> longerTickTags = new();
        readonly Dictionary<int, List<MCFunctionName>> unprocessedLongerTickTags = new();

        public IEnumerable<IDatapackFile> GeneratedFiles
            => new[] { loadTag, tickTag, trueLoadTag }
                .Union(longerTickTags.Values);
        
        /// <summary>
        /// Each [Tick(n&gt;1)] adds a method, and we equally distribute those
        /// (hence the index that will be in [0,tickrate)). This method returns
        /// all pairs that actually exist.
        /// </summary>
        public IEnumerable<(int tickrate, int index)> ActiveLongerTickPairs
            => longerTickTags.Keys;

        public override void GlobalPreProcess() {
            loadTag = new("minecraft", "load.json");
            loadTag.AddToTag(nameManager.SetupFileName);
            tickTag = new("minecraft", "tick.json");
            trueLoadTag = new(nameManager.manespace, $"{TrueLoadManager.TrueLoadTagname}.json");
        }

        public override void GlobalPostProcess() {
            // Evenly distribute the methods into their tags:
            // * If methodcount < tickrate, one method every tickrate/methodcount
            // * Otherwise, one method every tick, wrap around until methodcount < tickrate
            //   and do the previous point.
            // ~~if only tickrates were prime, then this would be so much smoother~~
            foreach (var kv in unprocessedLongerTickTags) {
                int tickRate = kv.Key;
                var funcList = kv.Value;
                int methodCount = funcList.Count;
                int fullIterations = (methodCount / tickRate) * tickRate;
                int remainderStepSize = 0;
                if (methodCount % tickRate != 0)
                    remainderStepSize = tickRate / (methodCount % tickRate);
                // Put in bins [0, 1, .., tickRate)
                //             [0, 1, .., tickRate)
                // until there is no more full bin.
                for (int i = 0; i < fullIterations; i++)
                    AddMethodToItsTag(funcList[i], tickRate, i % tickRate);
                // Put evenly distributed in [0, .., tickrate)
                if (remainderStepSize != 0)
                    for (int i = 0; i < funcList.Count - fullIterations; i++)
                        AddMethodToItsTag(funcList[fullIterations + i], tickRate, i * remainderStepSize);
            }
        }

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax method) {
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(LoadAttribute), out var _)) {
                loadTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, method, this));
            }
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(TrueLoadAttribute), out var _)) {
                trueLoadTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, method, this));
            }
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(TickAttribute), out var attrib)) {
                int tickRate = 1;
                if (attrib.ConstructorArguments.Length == 1) {
                    tickRate = (int)attrib.ConstructorArguments[0].Value;
                }
                if (tickRate < 1) {
                    AddCustomDiagnostic(DiagnosticRules.TickRateMustBePositive, method.GetLocation(), tickRate);
                    tickRate = 1;
                }
                MCFunctionName methodName = nameManager.GetMethodName(CurrentSemantics, method, this);
                if (tickRate == 1)
                    tickTag.AddToTag(methodName);
                else
                    AddMethodToProcessLater(methodName, tickRate);
            }
        }

        /// <summary>
        /// Assuming valid inputs, adds a method to <see cref="unprocessedLongerTickTags"/>
        /// creating elements if they don't exist yet.
        /// </summary>
        void AddMethodToProcessLater(MCFunctionName method, int tickrate) {
            if (!unprocessedLongerTickTags.TryGetValue(tickrate, out var functions)) {
                functions = new List<MCFunctionName>();
                unprocessedLongerTickTags.Add(tickrate, functions);
            }
            functions.Add(method);
        }

        /// <summary>
        /// Assuming valid inputs, adds a method to <see cref="longerTickTags"/>
        /// creating properly setup FunctionTags if they don't exist yet.
        /// </summary>
        void AddMethodToItsTag(MCFunctionName method, int tickrate, int index) {
            if (!longerTickTags.TryGetValue((tickrate, index), out var functiontag)) {
                functiontag = new(nameManager.manespace, $"{GetTickName(tickrate, index)}.json");
                // Add (nonexistent) method "compiled:--tick-{tickrate}-{index}--"
                // to this tag. (Much) later, Compiler will create (trivial) bodies
                //  `schedule #(its function tag) (tickrate)`
                longerTickTags.Add((tickrate, index), functiontag);
                // Yes this violates the construtor requirement.. ugh
                // I can't register a method that won't exist till the end of
                // the entire compilation process
                functiontag.AddToTag(new($"{nameManager.manespace}:{GetTickName(tickrate, index)}"));
            }
            functiontag.AddToTag(method);
        }

        /// <summary>
        /// For the 0 ≤ <paramref name="index"/>th &lt; <paramref name="tickrate"/>
        /// slot of a certain tickrate, returns the name of the function tag
        /// responsible for calling it. Excluding namespace, `#`, and `.json`.
        /// </summary>
        public static string GetTickName(int tickrate, int index)
            => $"internal/--tick-{tickrate}-{index}--";
    }
}
