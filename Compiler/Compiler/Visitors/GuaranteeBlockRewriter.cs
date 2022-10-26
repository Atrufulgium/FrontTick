using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This is to guarantee that if/else/for/do/while/etc's statements are
    /// always blocks, instead of occasionally single statements when the
    /// original source doesn't use any <tt>{ }</tt>.
    /// </para>
    /// <para>
    /// This also guarantees any goto label's statement is a block.
    /// </para>
    /// </summary>
    public class GuaranteeBlockRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            if (node.Else != null && node.Else.Statement is not BlockSyntax) {
                node = node.WithElse(ElseClause(Block(node.Else.Statement)));
            }
            return base.VisitIfStatement(node);
        }

        // Bit awkward how all of these are the literal same code, but they
        // don't share an interface.
        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            return base.VisitWhileStatement(node);
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            return base.VisitDoStatement(node);
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            return base.VisitForStatement(node);
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            return base.VisitForEachStatement(node);
        }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(Block(node.Statement));
            }
            return base.VisitUsingStatement(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            // Find a label, and collect all subsequent statements.
            LabeledStatementSyntax label = null;
            ICollection<StatementSyntax> insideLabel = new List<StatementSyntax>();

            foreach (var statement in node.Statements) {
                if (label == null) {
                    if (statement is LabeledStatementSyntax l) {
                        label = l;
                        continue;
                    }
                } else {
                    insideLabel.Add(statement);
                }
            }
            if (label != null && label.Statement is not BlockSyntax) {
                var labelBlock = Block(insideLabel);
                labelBlock = labelBlock.WithPrependedStatement(label.Statement);
                var newLabel = label.WithStatement(labelBlock);
                node = node.ReplaceNode(label, newLabel);
                // As the above replacement invalidates all equality in the
                // tree, awkwardly re-find the new label and remove all
                // statements after.
                int labelIndex;
                for (labelIndex = 0; node.Statements[labelIndex] is not LabeledStatementSyntax; labelIndex++) { }
                insideLabel = node.Statements.ToArray()[(labelIndex + 1)..];
                node = node.RemoveNodes(insideLabel, SyntaxRemoveOptions.KeepNoTrivia);
            }

            // Note that this will also walk over the just-introduced new block.
            return base.VisitBlock(node);
        }
    }

    /// <summary>
    /// A copy of <see cref="GuaranteeBlockRewriter"/> for running this phase
    /// an additional time (in <see cref="LoopsToGotoCategory"/>).
    /// </summary>
    // TODO: Do something so that this hilarious hack isn't needed.
    public class GuaranteeBlockRewriter2 : GuaranteeBlockRewriter { }
}
