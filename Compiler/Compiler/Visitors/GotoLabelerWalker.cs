using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// As <see cref="ProcessedToDatapackWalker"/> needs the information of all
    /// gotos, this class preprocesses that by telling every block what gotos
    /// are contained somewhere within and what labels are contained somewhere
    /// outside.
    /// </summary>
    public class GotoLabelerWalker : AbstractFullWalker {
        
        /// <summary>
        /// Suppose we have code of the form (or similar):
        /// <code>
        /// // Label(s) somewhere outside
        /// if (...) {
        ///     // Contains goto, among other things
        /// } else {
        ///     // Other code
        /// }
        /// // More code
        /// </code>
        /// This method tells us whether we have to functionally turn this into
        /// something like
        /// <code>
        /// // Label(s) somewhere outside
        /// if (...) {
        ///     // Goto is replaced by setting a flag
        /// } else {
        ///     // Other code
        /// }
        /// if (no flag set) {
        ///     // More code
        /// }
        /// </code>
        /// </summary>
        /// <remarks>
        /// Note: If both AfterScopeRequires[..] methods are relevant, we need
        /// to turn the final part into
        /// <code>
        /// if (no flag set) {
        ///     // More Code
        /// }
        /// if (label(s)' flag set) {
        ///     // Reset all flags
        ///     goto relevant label
        /// }
        /// </code>
        /// </remarks>
        public bool AfterScopeRequiresFlag(BlockSyntax scope) {
            if (!gotoStatementsPerBlock.TryGetValue(scope, out var deeper)
                || !gotoLabelsPerBlock.TryGetValue(scope, out var higher))
                return false;
            HashSet<string> deeperCopy = new HashSet<string>(deeper);
            deeperCopy.IntersectWith(higher);
            return deeperCopy.Count > 0;
        }

        /// <summary>
        /// Suppose we have code of the form (or similar):
        /// <code>
        /// // Label(s) in *this* scope
        /// if (...) {
        ///     // Contains goto, among other things
        /// } else {
        ///     // Other code
        /// }
        /// // More code
        /// </code>
        /// This method tells us whether we have to functionally turn this into
        /// something like
        /// <code>
        /// // Label(s) in *this* scope
        /// if (...) {
        ///     // Goto is replaced by setting a flag
        /// } else {
        ///     // Other code
        /// }
        /// if (label(s)' flag not set) {
        ///     // More code
        /// }
        /// if (label(s)' flag set) {
        ///     // Reset all flags
        ///     goto relevant label
        /// }
        /// </code>
        /// If this is the case, it returns all relevant labels' unique ID.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="AfterScopeRequiresFlag(BlockSyntax)"/>
        /// </remarks>
        public bool AfterScopeRequiresFlagConsume(
            BlockSyntax scope,
            out IEnumerable<int> consumedLabels
        ) {
            consumedLabels = Array.Empty<int>();
            if (!gotoStatementsPerBlock.TryGetValue(scope, out var deeper)
                || !gotoLabelsInBlock.TryGetValue(scope, out var current))
                return false;
            HashSet<string> deeperCopy = new HashSet<string>(deeper);
            deeperCopy.IntersectWith(current);
            consumedLabels =
                from label in deeperCopy
                select LabelToInt(label);
            return deeperCopy.Count > 0;
        }

        /// <summary>
        /// Whether a scope directly contains a specific label, and not in some
        /// higher or lower scope.
        /// </summary>
        public bool ScopeContainsLabel(BlockSyntax scope, string label)
            => gotoLabelsInBlock.TryGetValue(scope, out var labels)
            && labels.Contains(label);
        /// <inheritdoc cref="ScopeContainsLabel(BlockSyntax, string)"/>
        public bool ScopeContainsLabel(BlockSyntax scope, int label)
            => gotoLabelsInBlock.TryGetValue(scope, out var labels) 
            && (from stringLabel in labels select LabelToInt(stringLabel)).Contains(label);

        /// <summary>
        /// We will need a correspondence
        /// <code>
        ///     goto label name ⇔ positive integer
        /// </code>
        /// for scoreboard purposes. This fulfills that need.
        /// </summary>
        public int LabelToInt(string label) => gotos[label];
        private Dictionary<string, int> gotos = new();

        /// <summary>
        /// In any block/scope, says what goto statements are found somewhere
        /// within, in a nested deeper scope.
        /// </summary>
        /// <remarks>
        /// Only if both the statement (<see cref="gotoStatementsPerBlock"/>)
        /// and the label (<see cref="gotoLabelsPerBlock"/>) are active for a
        /// block, do we need to do anything later.
        /// </remarks>
        private Dictionary<BlockSyntax, HashSet<string>> gotoStatementsPerBlock = new();
        /// <summary>
        /// In any block/scope, says what goto labels are found in some higher
        /// scope (the opposite direction from <see cref="gotoStatementsPerBlock"/>).
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="gotoStatementsPerBlock"/>
        /// </remarks>
        private Dictionary<BlockSyntax, HashSet<string>> gotoLabelsPerBlock = new();
        /// <summary>
        /// In any block/scope, says what goto labels are in statements in
        /// exactly this scope, not higher, not lower.
        /// </summary>
        private Dictionary<BlockSyntax, HashSet<string>> gotoLabelsInBlock = new();

        public override void VisitGotoStatement(GotoStatementSyntax gotoNode) {
            base.VisitGotoStatement(gotoNode);
            // TODO: Implement not just goto labels (goto case/default also).
            if (gotoNode.Kind() != SyntaxKind.GotoStatement)
                throw new NotImplementedException("TODO: Implement not just goto labels (goto case/default also).");

            string identifier = ((IdentifierNameSyntax)gotoNode.Expression).Identifier.Text;

            // Add the fact that this goto statement exists to all higher blocks.
            foreach (var block in
                from node in gotoNode.Ancestors()
                where node is BlockSyntax
                select (BlockSyntax)node) {
                // The actual add is a bit annoying because the value may not exist.
                if (!gotoStatementsPerBlock.TryGetValue(block, out HashSet<string> blockGotos)) {
                    blockGotos = new();
                    gotoStatementsPerBlock.Add(block, blockGotos);
                }
                blockGotos.Add(identifier);
            }
        }

        public override void VisitLabeledStatement(LabeledStatementSyntax labelNode) {
            base.VisitLabeledStatement(labelNode);

            // Basically the same logic as
            /// <see cref="VisitGotoStatement(GotoStatementSyntax)"/>
            string identifier = labelNode.Identifier.Text;
            // Need identifier <=> positive int mapping later
            if (!gotos.ContainsKey(identifier))
                gotos.Add(identifier, gotos.Count + 1);

            var finer = from node in labelNode.DescendantNodes()
                        where node is BlockSyntax
                        select (BlockSyntax)node;
            var coarser = labelNode.Ancestors().OfType<BlockSyntax>().First();

            // All finer scopes
            foreach (var block in finer) {
                if (!gotoLabelsPerBlock.TryGetValue(block, out HashSet<string> blockLabels)) {
                    blockLabels = new();
                    gotoLabelsPerBlock.Add(block, blockLabels);
                }
                blockLabels.Add(identifier);
            }
            // The single outside rougher scope.
            if (!gotoLabelsInBlock.TryGetValue(coarser, out HashSet<string> blockLabelDefs)) {
                blockLabelDefs = new();
                gotoLabelsInBlock.Add(coarser, blockLabelDefs);
            }
            blockLabelDefs.Add(identifier);
        }
    }
}
