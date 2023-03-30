using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any expression of the form <tt>a + b</tt> into <tt>#ADD(a,b)</tt>
    /// and <tt>a += b</tt> into <tt>a = #ADD(a,b)</tt>.
    /// </para>
    /// <para>
    /// Assumes this transformation can happen *in all cases but one*:
    /// *every* operation but any <tt>int ∘ int</tt> must have a underlying
    /// method. Here ∘∈{+=, -=, *=, /=, %=, ==, !=, &gt;=, &lt;=, &gt;, &lt;}.
    /// </para>
    /// <para>
    /// (Yes, even <tt>int ∘ int</tt> is disallowed.)
    /// </para>
    /// </summary>
    // TODO: Don't give exceptional status to ints. Instead, implement MCInt and use RunRaw().
    public class OperatorsToMethodCallsRewriter : AbstractFullRewriter<CopyOperatorsToNamedRewriter> {

        // ..attributes exist lol. Don't process those, even though they may
        // use `|` or something.
        public override SyntaxNode VisitAttribute(AttributeSyntax node) {
            return node;
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node) {
            var op = (IBinaryOperation)CurrentSemantics.GetOperation(node);
            if (op.OperatorMethod == null) {
                // No operator method. Not allowed except a few integer ones.
                if (CurrentSemantics.TypesMatch(op.LeftOperand.Type, typeof(int))
                    && CurrentSemantics.TypesMatch(op.RightOperand.Type, typeof(int))
                    && node.OperatorToken.Text is "==" or "!=" or ">=" or "<=" or ">" or "<")
                    return base.VisitBinaryExpression(node);
                throw CompilationException.OperatorsRequireUnderlyingMethod;
            }
            var containingType = op.OperatorMethod.ContainingType;
            var methodName = NameOperatorsCategory.GetMethodName(node.OperatorToken.Text);
            return InvocationExpression(
                MemberAccessExpression(containingType, methodName),
                ArgumentList(
                    (ExpressionSyntax)Visit(node.Left),
                    (ExpressionSyntax)Visit(node.Right)
                )
            );
        }

        private readonly List<string> intAllowed = new() { "+=", "-=", "*=", "/=", "%=", "==", "!=", ">=", "<=", ">", "<" };
        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (node.Kind() == SyntaxKind.SimpleAssignmentExpression)
                return base.VisitAssignmentExpression(node);

            // TODO: We've excluded ISimple with the above, handle ICompound here, but ICoalesce and IDesconstruction also exist. Far future.
            var op = (ICompoundAssignmentOperation)CurrentSemantics.GetOperation(node);
            if (op.OperatorMethod == null) {
                if (intAllowed.Contains(node.OperatorToken.Text))
                    return base.VisitAssignmentExpression(node);
                throw CompilationException.OperatorsRequireUnderlyingMethod;
            }

            var containingType = op.OperatorMethod.ContainingType;
            var methodName = NameOperatorsCategory.GetMethodName(node.OperatorToken.Text[0..^1]);
            var left = (ExpressionSyntax)Visit(node.Left);
            var right = (ExpressionSyntax)Visit(node.Right);
            return AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                left,
                InvocationExpression(
                    MemberAccessExpression(containingType, methodName),
                    ArgumentList(
                        left,
                        right
                    )
                )
            );
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
            // Alow literals like "-3".
            if (node.Operand is LiteralExpressionSyntax literal
                && literal.Kind() == SyntaxKind.NumericLiteralExpression)
                return node;

            var op = (IUnaryOperation)CurrentSemantics.GetOperation(node);
            return HandleUnary(op, node.OperatorToken.Text, node.Operand);
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
            if (node.Operand is LiteralExpressionSyntax literal
                && literal.Kind() == SyntaxKind.NumericLiteralExpression)
                return node;

            var op = (IUnaryOperation)CurrentSemantics.GetOperation(node);
            return HandleUnary(op, node.OperatorToken.Text, node.Operand);
        }

        SyntaxNode HandleUnary(IUnaryOperation op, string opText, ExpressionSyntax operand) {
            if (op.OperatorMethod == null)
                throw CompilationException.OperatorsRequireUnderlyingMethod;

            var containingType = op.OperatorMethod.ContainingType;
            var methodName = NameOperatorsCategory.GetMethodName(opText);
            return InvocationExpression(
                MemberAccessExpression(containingType, methodName),
                ArgumentList(
                    (ExpressionSyntax)Visit(operand)
                )
            );
        }
    }
}
