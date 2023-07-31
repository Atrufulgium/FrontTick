using Atrufulgium.FrontTick.Compiler.Datapack;
using MCMirror;
using MCMirror.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Walks the tree to collect all methods with <see cref="LoadAttribute"/>,
    /// <see cref="TrueLoadAttribute"/>, and <see cref="TickAttribute"/>.
    /// </summary>
    public class LoadTickWalker : AbstractFullWalker {

        private FunctionTag loadTag;
        private FunctionTag tickTag;
        private FunctionTag trueLoadTag;

        public IEnumerable<IDatapackFile> GeneratedFiles => new[] { loadTag, tickTag, trueLoadTag };

        public override void GlobalPreProcess() {
            loadTag = new("minecraft", "load.json");
            tickTag = new("minecraft", "tick.json");
            trueLoadTag = new(nameManager.manespace, $"{TrueLoadManager.TrueLoadTagname}.json");
        }

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax method) {
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(LoadAttribute), out var _)) {
                loadTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, method, this));
            }
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(TrueLoadAttribute), out var _)) {
                trueLoadTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, method, this));
            }
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(TickAttribute), out var attrib)) {
                //int tickRate = 1;
                //if (attrib.ConstructorArguments.Length == 1) {
                //    throw new System.NotImplementedException("hoi todo");
                //}
                tickTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, method, this));
            }
        }
    }
}
