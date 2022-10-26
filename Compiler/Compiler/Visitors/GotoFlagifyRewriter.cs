using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// .mcfunction has a problem of being able to run code multiple times if
    /// you're not careful -- implementing <tt>goto</tt> as simply calling the
    /// function corresponding to that scope becomes incorect then.
    /// </para>
    /// <para>
    /// This step turns <tt>goto</tt> into flags that are read later in a
    /// way that goto executes correctly if you run <see cref="ProcessedToDatapackWalker"/>
    /// after this (with optionally other steps that do not modify control flow,
    /// scoping, goto labels and targets, or return statements inbetween).
    /// </para>
    /// </summary>
    public class GotoFlagifyRewriter : AbstractFullRewriter<GuaranteeBlockRewriter, LoopsToGotoCategory, ReturnRewriter> {
        // TODO: This is incorrect because ReturnRewriter's return breaks the label assumption. Not majorly incorrect though. (Also remove the ReturnRewriter todo)

        // The basic idea is that any code of the form
        //     // Label here
        //     {
        //       {
        //         ..
        //             {
        //               // Has a goto
        //             }
        //          ..
        //       }
        //     }
        //     // Or label here
        // (where the scopes may have code between the various open/close brackets)
        // gets rewritten to
        //     // Label here
        //     {
        //       {
        //         ..
        //             {
        //               // Goto replaced by flag
        //             }
        //             if (no flag) { rest of this scope }
        //          ..
        //       }
        //       if (no flag) { rest of this scope }
        //     }
        //     if (no flag) { rest of this scope }
        //     if (flag) { reset flag and goto }
        //     // Or label here
        // The problem is of course various sources of complexity.
        // Consider for instance the goto and the label in scopes pretty far
        // apart (yes I know this is illegal in c#, but I generate it). In this
        // case, the `if (flag) { reset flag and goto }` should be put in the
        // finest scope containing both the goto and the label.
        // (The label here counts as being part of the block it labels.)
        //
        // For if-else blocks it is fine to put the flag-checking after the
        // full if-else statement, not interrupt it.
        // For labeled blocks, it is fine to not put any flag-checking after
        // as the assumption is that there is no code after any `label:{}` in
        // the label's scope.
        // For loops -- they don't exist. The LoopsToGotoCategory ensures we
        // only need to care about if-statements. The only two scope-increasing
        // mechanisms at this point are labeled blocks and if-else branching,
        // so we only need to care about one! Hooray.
        //
        // For multiple flags and labels across various scopes, we need to do
        // both -- a check of `if (no flag for any goto) { rest of the scope }`
        // and a check `if (specific flag(s)) { reset flag(s), goto relevant goto}`.
        // This is implemented as going through all flags and writing to
        // `#FLAGFOUND` if we find any flag.
        // TODO: This can be improved once more complex if-statements are supported.
        //
        // The flags are implemented as a *single* scoreboard value that gets
        // reset after every jump, with the flag specifying the currently
        // "active" goto jump.

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            // Skip the method if there's nothing. Otherwise add a flag declaration.
            var gotos = ContainedGotoStatements(node.Body);
            if (gotos.IsEmpty())
                return node;

            node = node.WithBody(
                node.Body.WithPrependedStatement(
                    ExpressionStatement(
                        DeclarationExpression(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingleVariableDesignation(NameManager.GetGotoFlagName())
                        )
                    )
                )
            );
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            // Skip processing the block if there's nothing.
            var gotos = ContainedGotoStatements(node);
            if (gotos.IsEmpty())
                return node;

            // By assumption, labels are done. We need to now rewrite any if-
            // statement containing gotos as explained above.

            // This is the offending statement containing the first goto.
            IfStatementSyntax firstGotoer = null;
            // This contains all statements before the first if-else with goto
            // (excluding firstGotoer).
            List<StatementSyntax> before = new();
            // This contains all statements after the first if-else with goto
            // (excluding firstGotoer).
            List<StatementSyntax> after = new();

            foreach(var s in node.Statements) {
                // Replace any goto statement with an assignment to the flag.
                // This is safe, as any analysis considers the *original* node.
                // (Also this is at most the last statement due to the "goto
                //  may only be the final statement of a block"-rule, but eh.)
                StatementSyntax statement = s;
                if (s is GotoStatementSyntax got) {
                    statement = SimpleAssignmentStatement(
                        IdentifierName(NameManager.GetGotoFlagName()),
                        NumericLiteralExpression(
                            GotoLabelToScoreboardID(got)
                        )
                    );
                }

                // If we're done and just scanning the rest of the scope
                if (firstGotoer != null) {
                    after.Add(statement);
                    continue;
                }
                // We're not done and actively searching the goto
                if (statement is IfStatementSyntax ifst) {

                    // Branches and labels need sub-processing.
                    ifst = (IfStatementSyntax)base.VisitIfStatement(ifst);
                    statement = ifst;

                    if (!gotos.IsEmpty()) {
                        firstGotoer = ifst;
                        continue;
                    }
                }
                // Branches and labels need sub-processing.
                if (statement is LabeledStatementSyntax label) {
                    statement = (LabeledStatementSyntax) base.VisitLabeledStatement(label);
                }

                // We're not done and not a goto-if-else.
                before.Add(statement);
            }
            
            // Don't forget to update the new block!
            BlockSyntax afterBlock = (BlockSyntax)VisitBlock(Block(after));

            // Now create a new block, of either of the form
            //      <before>
            //      <goto-containing ifst>
            //      if (#GOTOFLAG == 0) {
            //          <after>
            //      }
            //      if (#GOTOFLAG == <x>) {      ⎫ For each flag such that the
            //          #GOTOFLAG = 0;           ⎪ label is just one scope
            //          goto <label x>           ⎪ coarser.
            //      }                            ⎭ 
            // or
            //      <before>
            //      <goto-containing ifst>
            //      if (#GOTOFLAG == <x>) {
            //          #GOTOFLAG = 0;
            //          goto <label x>
            //      }
            // depending on whether there even is an "after".
            // Note we only consider flags with label < scope < goto or
            // scope < label,goto.
            // The distance label--goto needs to be filled with flags, the rest
            // doesn't.
            List<StatementSyntax> newStatements = before; // yes, just a rename
            if (firstGotoer != null)
                newStatements.Add(firstGotoer);

            if (after.Count > 0) {
                newStatements.Add(
                    // if (#GOTOFLAG == 0) {
                    //     // Rest of the scope
                    // }
                    IfStatement(
                        BinaryEqualsExpression(
                            IdentifierName(NameManager.GetGotoFlagName()),
                            NumericLiteralExpression(0)
                        ),
                        afterBlock
                    )
                );
            }

            // Note: the introduced goto statement in this does not matter
            // for analysis anymore. The only analysis after this is on
            // the "after" branch, which is unrelated to this.
            foreach (var label in gotos) {
                // We need to check the *modified* tree here.
                // But the parent of the *original* node is relevant also for
                // label checking.
                // Note that the parent of any `VisitBlock(Block(after))` is
                // null -- this is fine as its real parent is an if statement
                // and not a label.
                if (IsFinestScopeContainingGotoAndLabel(Block(newStatements), node.Parent, label)) {
                    // Note that if we're going to the returning label, we
                    // don't actually have to jump as it's already irrelevant.
                    StatementSyntax jumpStatement = GotoStatement(label);
                    if (label == NameManager.GetRetGotoName())
                        jumpStatement = EmptyStatement();

                    newStatements.Add(
                        // if (#GOTOFLAG == <flag id>) {
                        //     #GOTOFLAG = 0;
                        //     goto <flagged label>
                        // }
                        IfStatement(
                            BinaryEqualsExpression(
                                IdentifierName(NameManager.GetGotoFlagName()),
                                NumericLiteralExpression(GotoLabelToScoreboardID(label))
                            ),
                            Block(
                                SimpleAssignmentStatement(
                                    IdentifierName(NameManager.GetGotoFlagName()),
                                    NumericLiteralExpression(0)
                                ),
                                jumpStatement
                            )
                        )
                    );
                }
            }

            return Block(newStatements);
        }

        /// <summary>
        /// For every goto statement in <paramref name="node"/> and deeper
        /// scopes, returns the associated string label. This includes already-
        /// transformed gotostatements that are of the form
        /// <tt>#GOTOFLAG = &lt;x&gt;</tt>
        /// </summary>
        private HashSet<string> ContainedGotoStatements(BlockSyntax node) {
            HashSet<string> ret = new();
            foreach (var descendant in node.DescendantNodes()) {
                if (descendant is GotoStatementSyntax l) {
                    ret.Add(l.Identifier());
                } else if (descendant is AssignmentExpressionSyntax a
                    && a.Kind() == SyntaxKind.SimpleAssignmentExpression
                    && a.Left is IdentifierNameSyntax id
                    && id.Identifier.Text == NameManager.GetGotoFlagName()) {
                    var textnum = ((LiteralExpressionSyntax)a.Right).Token.Text;
                    var intnum = int.Parse(textnum);
                    if (intnum != 0) // Don't wanna add the resetter =0 lol
                        ret.Add(ScoreboardIDToGotoLabel(intnum));
                }
            }
            return ret;
        }

        /// <summary>
        /// For every label in <paramref name="node"/> and deeper scopes,
        /// returns the associated string label.
        /// </summary>
        /// <remarks>
        /// This does *not* return the immediately outer label if the node is
        /// the Expression of a LabeledStatementSyntax!
        /// </remarks>
        private static HashSet<string> ContainedLabels(BlockSyntax node)
            => new(
                from descendant in node.DescendantNodes()
                where descendant is LabeledStatementSyntax
                select ((LabeledStatementSyntax)descendant).Identifier.Text
               );

        /// <summary>
        /// Checks if this node is a LabeledStatementSyntax's Expression, and
        /// if so, returns the corresponding label.
        /// </summary>
        private static bool TryGetBlockLabel(BlockSyntax node, out string label) {
            if (node.Parent is LabeledStatementSyntax l) {
                label = l.Identifier.Text;
                return true;
            }
            label = null;
            return false;
        }

        /// <summary>
        /// Checks whether this is the finest scope for a label containing both
        /// a goto statement somewhere and a label.
        /// </summary>
        /// <remarks>
        /// This definition needs thinking if a single label has multiple gotos,
        /// especially in unrelated scope. Generalise to contain *all*? Any?
        /// TODO: This needs some heavy thinking and testing.
        /// </remarks>
        private bool IsFinestScopeContainingGotoAndLabel(BlockSyntax node, SyntaxNode nodeParent, string label) {
            // Yes this performance and style is terrible.
            // No I don't care currently, it has to *work* first.
            // Fancier graphs are later.
            bool containsGoto = ContainedGotoStatements(node).Contains(label);
            bool containsLabel = ContainedLabels(node).Contains(label);
            containsLabel |= nodeParent is LabeledStatementSyntax l && l.Identifier.Text == label;
            bool containsBoth = containsGoto && containsLabel;
            if (!containsBoth)
                return false;

            List<BlockSyntax> check = new(2);
            foreach(var statement in node.Statements) {
                // If any child label/branch *also* contains both, it's false.
                check.Clear();
                if (statement is LabeledStatementSyntax l2)
                    check.Add((BlockSyntax)l2.Statement);
                else if (statement is IfStatementSyntax i) {
                    check.Add((BlockSyntax)i.Statement);
                    if (i.Else != null)
                        check.Add((BlockSyntax)i.Else.Statement);
                }

                foreach(var b in check) {
                    containsGoto = ContainedGotoStatements(b).Contains(label);
                    containsLabel = ContainedLabels(b).Contains(label);
                    containsLabel |= TryGetBlockLabel(b, out string l3) && l3 == label;
                    containsBoth = containsGoto && containsLabel;
                    if (containsBoth)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Do not use directly; instead, use
        /// <see cref="GotoLabelToScoreboardID(string)"/>.
        /// </summary>
        readonly Dictionary<string, int> gotoLabelIDs = new();
        readonly Dictionary<int, string> IDGotoLabels = new();

        /// <summary>
        /// Gives a correspondence
        /// <code>
        ///     goto label name ⇔ positive integer
        /// </code>
        /// </summary>
        public int GotoLabelToScoreboardID(string label) {
            if (!gotoLabelIDs.TryGetValue(label, out int id)) {
                // +1 as "0" represents "none"
                id = gotoLabelIDs.Count + 1;
                gotoLabelIDs.Add(label, id);
                IDGotoLabels.Add(id, label);
            }
            return id;
        }

        public bool TryScoreboardIDToGotoLabel(int id, out string label)
            => IDGotoLabels.TryGetValue(id, out label);

        public string ScoreboardIDToGotoLabel(int id)
            => IDGotoLabels[id];

        /// <inheritdoc cref="GotoLabelToScoreboardID(string)"/>
        private int GotoLabelToScoreboardID(GotoStatementSyntax got)
            => GotoLabelToScoreboardID(got.Identifier());

        /// <inheritdoc cref="GotoLabelToScoreboardID(string)"/>
        private int GotoLabelToScoreboardID(LabeledStatementSyntax label)
            => GotoLabelToScoreboardID(label.Identifier.Text);
    }
}
