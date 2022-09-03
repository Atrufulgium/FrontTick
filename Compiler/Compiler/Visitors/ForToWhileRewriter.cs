using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This is to transform for loops into while loops.
    /// </para>
    /// <para>
    /// Transform
    /// <code>
    ///     for (init; cond; update) {
    ///         // Stuff
    ///     }
    /// </code>
    /// into something like
    /// <code>
    ///     {
    ///         init;
    ///         while(cond) {
    ///             // Stuff
    ///             update;
    ///         }
    ///     }
    /// </code>
    /// where all <tt>continue;</tt> within the scope of the loop must
    /// be replaced with <tt>update; continue;</tt> for correctness.
    /// </para>
    /// </summary>
    public class ForToWhileRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {
        // TODO: Depend on something that pulls declaration to the method start (for easier for loops)

        // This tracks whether we are in a for -> while conversion (null if not
        // and we're in another looptype), and if so, what the increment is.
        // This increment is to be added before `continue;` statements.
        readonly Stack<SeparatedSyntaxList<ExpressionSyntax>?> incrementorsPerLoop = new();

        public override void PreProcess() {
            incrementorsPerLoop.Clear();
            // Keep a base of "false" in order to not have to care about size.
            incrementorsPerLoop.Push(null);
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node) {
            // Note: The first "argument" of for loops are a bit hacky. Either:
            // * it's a declaration and you use ForStatementSyntax.Declaration; or
            // * it's an expression (assignment), and you use .Initializers.
            // In order not to suffer from this, simply disallow the first at
            // this point -- handle extracting declarations to the top earlier.
            if (node.Declaration != null)
                throw CompilationException.LoopsToGotoForInitNoDeclarationsAllowed;

            var whileBody = (BlockSyntax)node.Statement;
            foreach (var inc in node.Incrementors)
                whileBody = whileBody.WithAppendedStatement(ExpressionStatement(inc));

            BlockSyntax body = Block();
            foreach (var init in node.Initializers)
                body = body.WithAppendedStatement(ExpressionStatement(init));

            // Doing double work this way but *eh*.
            // Need this to easily replace `continue;` with `update; continue;`
            incrementorsPerLoop.Push(node.Incrementors);
            whileBody = (BlockSyntax)VisitBlock(whileBody);
            incrementorsPerLoop.Pop();

            // Now walk the tree properly
            body = body.WithAppendedStatement(WhileStatement(node.Condition, whileBody));
            return VisitBlock(body);
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
            => VisitIrrelevantLoop(node);

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
            => VisitIrrelevantLoop(node);

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
            => VisitIrrelevantLoop(node);

        public override SyntaxNode VisitContinueStatement(ContinueStatementSyntax node) {
            var incrementors = incrementorsPerLoop.Peek();
            if (!incrementors.HasValue)
                return node;

            // We are part of a for -> while conversion so we need to add all
            // incrementors before the continue;
            BlockSyntax body = Block();
            foreach (var inc in incrementors)
                body = body.WithAppendedStatement(ExpressionStatement(inc));
            body = body.WithAppendedStatement(ContinueStatement());
            return body;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            return ((BlockSyntax) base.VisitBlock(node)).Flattened();
        }

        /// <summary>
        /// Visit a loop that is *not* a for loop. This is only used to keep
        /// track of what part of the tree is in scope of a relevant loop.
        /// </summary>
        SyntaxNode VisitIrrelevantLoop(SyntaxNode node) {
            // Signify that we are no longer in scope of a for->while conversion
            incrementorsPerLoop.Push(null);
            // As base.Visit calls this.Visit[loops], this is somewhat ugly
            if (node is DoStatementSyntax doNode)
                node = base.VisitDoStatement(doNode);
            else if (node is WhileStatementSyntax whileNode)
                node = base.VisitWhileStatement(whileNode);
            else if (node is ForEachStatementSyntax foreachNode)
                node = base.VisitForEachStatement(foreachNode);
            incrementorsPerLoop.Pop();
            return node;
        }
    }
}
