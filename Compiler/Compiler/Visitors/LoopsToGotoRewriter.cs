using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to transform for/while/do while loops into goto loops.
    /// </summary>
    public class LoopsToGotoRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {
        // TODO: LoopsToGotoRewriter optimisation opportunities:
        // * If a loop's final statement is `break;`, we do not need a goto.
        //   Simply exit the generated if-statement.
        // * If a loop ends with a branching tree that ends the block via means
        //   of `break`, `continue`, `goto`, or `return`, we still introduce
        //   an extra goto at the end. This case is fairly rare however.

        string currentContinueLabel = null;
        string currentBreakLabel = null;
        Stack<bool> foundBreakPerWhile = new();

        public override void PreProcess() {
            foundBreakPerWhile.Clear();
            // Keep a base of "false" in order to not have to care about size.
            foundBreakPerWhile.Push(false);
        }

        /// <summary>
        /// Transform
        /// <code>
        ///     while(condition) {
        ///         // Stuff
        ///     }
        /// </code>
        /// into something like
        /// <code>
        ///     WhileStart:
        ///     if (condition) {
        ///         // Stuff
        ///         goto WhileStart;
        ///     }
        ///     WhileBreak: // Only if there's a "break".
        /// </code>
        /// Any contained <tt>break;</tt> statements get turned into
        /// <tt>goto WhileBreak;</tt> while any <tt>continue;</tt> statements
        /// get turned into <tt>goto WhileStart;</tt>.
        /// </summary>
        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax whilenode) {
            (string startLabel, string breakLabel) = GetUniqueLabels();
            // Here we use that all looping mechanisms have block bodies.
            var labeledLoop = LabeledStatement(
                startLabel,
                IfStatement(
                    whilenode.Condition,
                    ((BlockSyntax)whilenode.Statement).WithAppendedStatement(
                        GotoStatement(startLabel)
                    )
                )
            );

            // When visiting children, we have to know what labels to go to, and
            // whether we've encountered a break (in which case we should return
            // with a break label).
            (string oldContinue, string oldBreak) = (currentContinueLabel, currentBreakLabel);
            (currentContinueLabel, currentBreakLabel) = (startLabel, breakLabel);
            foundBreakPerWhile.Push(false);
            var node = base.VisitLabeledStatement(labeledLoop);
            (currentContinueLabel, currentBreakLabel) = (oldContinue, oldBreak);
            bool foundBreak = foundBreakPerWhile.Pop();

            // If we have a goto as the very last line of the original loop, we
            // would have two sequential gotos, which smells of incorrect code.
            // (Note that with the above-introduced GotoStatementSyntax it
            //  would now be the second-to-last.)
            // In this case, simply remove the goto added above.
            // (I can't do this before the `base.Visit()` unfortunately --
            //  consider that e.g. `break;` is converted into goto.)

            // Labels and such may have been introduced, but by construction
            // the first child if-statement corresponds to the created if
            // statement above.
            var block = (BlockSyntax) node.DescendantNodes().OfType<IfStatementSyntax>().First().Statement;
            var originalLast = block.Statements.Reverse().Skip(1).First(); // Original last statement.
            if (originalLast is GotoStatementSyntax) {
                var last = block.Statements.Last();
                node = node.ReplaceNode(
                    block,
                    block.RemoveNode(last, SyntaxRemoveOptions.AddElasticMarker)
                );
            }

            if (foundBreak) {
                // Simply add a label after for the break we have to go to.
                var withBreak = Block(
                    (StatementSyntax) node,
                    LabeledStatement(
                        breakLabel,
                        EmptyStatement()
                    )
                );
                return withBreak;
            } else {
                return node;
            }
            // Note that we now may have returned a BlockStatement. To maintain
            // the invariant that we do not have nested blocks, we continue in
            /// <see cref="VisitBlock(BlockSyntax)"/>
            // to flatten it.
        }

        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node) {
            foundBreakPerWhile.Pop();
            foundBreakPerWhile.Push(true);
            return GotoStatement(currentBreakLabel);
        }

        public override SyntaxNode VisitContinueStatement(ContinueStatementSyntax node) {
            return GotoStatement(currentContinueLabel);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            // As mentioned before, we may be in a situation where the loop
            // modifications introduce nested blocks for no reason. Solve that
            // here.
            return ((BlockSyntax) base.VisitBlock(node)).Flattened();
        }

        private int uniqueID = 0;
        private (string, string) GetUniqueLabels() => ($"whilestart{uniqueID}", $"whilebreak{uniqueID++}");
    }
}
