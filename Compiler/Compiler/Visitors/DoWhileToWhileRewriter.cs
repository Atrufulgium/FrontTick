using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This is to transform for loops into while loops.
    /// </para>
    /// <para>
    /// Transform
    /// <code>
    ///     do {
    ///         // Stuff
    ///     } while (cond);
    /// </code>
    /// into something like
    /// <code>
    ///     while (true) {
    ///         // Stuff
    ///         if (cond) {continue;}
    ///         else {break};
    ///     }
    /// </code>
    /// </para>
    /// </summary>
    public class DoWhileToWhileRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {
        // TODO: Once boolean arithmetic is implemented, do the check better.
        // (Or not. The generated .mcfunction is good anyway.)

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node) {
            var body = (BlockSyntax) node.Statement;
            // Do note that we already require all branches to be blocks.
            // Maintain that.
            body = body.WithAppendedStatement(
                IfStatement(
                    node.Condition,
                    Block(ContinueStatement()),
                    ElseClause(
                        Block(BreakStatement())
                    )
                )
            );
            var whileNode = WhileStatement(
                LiteralExpression(SyntaxKind.TrueLiteralExpression),
                body
            );

            return base.VisitWhileStatement(whileNode);
        }
    }
}
