using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns all short circuiting
    /// <code>
    ///     a ∘= (left) &amp;&amp; (right);
    /// 
    ///     o ∘= (left) || (right);
    /// </code>
    /// operators into explicit
    /// <code>
    ///     a ∘= (left);
    ///     if (a) {
    ///         a &amp;= (right);
    ///     }
    ///     
    ///     o ∘= (left);
    ///     if (!o) {
    ///         o |= (right);
    ///     }
    /// </code>
    /// for boolean expressions <c>(left)</c> and <c>(right)</c>.
    /// </para>
    /// <para>
    /// ...or, well, it ought to. However, it's a bit of a pain to implement
    /// with cases such as `f(g(a && b))` needing their own extraction
    /// procedure <i>independent</i> from <see cref="FlattenNestedCallsRewriter"/>
    /// (as handling short circuiting afterwards is not justified).
    /// </para>
    /// <para>
    /// So for now, all it does is <b>replace any short-circuit operator with
    /// its logical variant and gives a warning</b>.
    /// </para>
    /// </summary>
    public class ShortCircuitOperatorRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node) {
            bool shortCircuit = true;
            if (node.IsKind(SyntaxKind.LogicalAndExpression))
                node = node.WithOperatorToken(Token(SyntaxKind.AmpersandToken));
            else if (node.IsKind(SyntaxKind.LogicalOrExpression))
                node = node.WithOperatorToken(Token(SyntaxKind.BarToken));
            else
                shortCircuit = false;

            if (shortCircuit)
                AddCustomDiagnostic(DiagnosticRules.ShortCircuitingUnsupported, node.GetLocation());

            return base.VisitBinaryExpression(node);
        }
    }
}
