using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Rewrite a tree of unary and binary expressions in statements into
    /// multiple statements containing only one. These expressions must be part
    /// of statements and not found in arguments of functions or loops.
    /// Assignments must be simple <tt>=</tt> already and not <tt>+=</tt> etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transforms statements of the form:
    /// <code>
    /// Thing.Something = -(i + 2) * 3 + 4 * 5 * (7 + 8 | 9);
    /// </code>
    /// into:
    /// <code>
    /// // (At the top of the scope)
    /// int #temp1, #temp2, #temp3, #temp4, #temp5, #temp6, #temp7;
    /// // ...
    /// #temp1 = i + 2;
    /// #temp2 = -#temp1;
    /// #temp3 = #temp2 * 3;
    /// #temp4 = 4 * 5;
    /// #temp5 = 7 + 8;
    /// #temp6 = #temp5 | 9;
    /// #temp7 = #temp4 * #temp6;
    /// Thing.Something = #temp3 + #temp7;
    /// </code>
    /// </para>
    /// </remarks>
    // TODO: The above comments are outdated, and I instead need ∘= and not = a ∘ b
    public class ArithmeticFlattenRewriter : AbstractFullWalker {

        public override void VisitBinaryExpression(BinaryExpressionSyntax node) {
            base.VisitBinaryExpression(node);
            // https://stackoverflow.com/a/28817321
        }
    }
}
