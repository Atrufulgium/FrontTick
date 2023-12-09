using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any expression of the form <tt>a + b</tt> into <tt>#ADD(a,b)</tt>
    /// and <tt>a += b</tt> into <tt>a = #ADD(a,b)</tt>.
    /// </para>
    /// </summary>
    public class OperatorsToMethodCallsRewriter : AbstractFullRewriter<CopyOperatorsToNamedRewriter> {

        // ..attributes exist lol. Don't process those, even though they may
        // use `|` or something.
        public override SyntaxNode VisitAttribute(AttributeSyntax node) {
            return node;
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node) {
            var (fullyQualified, name) = NameOperatorsCategory.ParseOperator(CurrentSemantics, node);

            var fullyQualifiedName = $"{fullyQualified}.{name}";
            return InvocationExpression(
                MemberAccessExpression(fullyQualifiedName),
                ArgumentList(
                    (ExpressionSyntax)Visit(node.Left),
                    (ExpressionSyntax)Visit(node.Right)
                )
            );
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (node.Kind() == SyntaxKind.SimpleAssignmentExpression)
                return base.VisitAssignmentExpression(node);

            var (fullyQualified, name) = NameOperatorsCategory.ParseOperator(CurrentSemantics, node);
            var fullyQualifiedName = $"{fullyQualified}.{name}";

            var left = (ExpressionSyntax)Visit(node.Left);
            var right = (ExpressionSyntax)Visit(node.Right);
            return AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                left,
                InvocationExpression(
                    MemberAccessExpression(fullyQualifiedName),
                    ArgumentList(
                        left,
                        right
                    )
                )
            );
        }

        // +x -x !x ~x ++x --x ^x (T)x await x &x *x true(x) false(x)
        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
            // Allow literals like "-3" to be parsed by ProcessedToDatapack later.
            if (node.Operand is LiteralExpressionSyntax literal
                && literal.Kind() == SyntaxKind.NumericLiteralExpression
                && (node.IsKind(SyntaxKind.UnaryPlusExpression)
                 || node.IsKind(SyntaxKind.UnaryMinusExpression)))
                return node;

            if (node.IsKind(SyntaxKind.PreIncrementExpression)
                || node.IsKind(SyntaxKind.PreDecrementExpression))
                throw new System.ArgumentException("++x and --x should already be handled.");

            var (fullyQualified, name) = NameOperatorsCategory.ParseOperator(CurrentSemantics, node);
            var fullyQualifiedName = $"{fullyQualified}.{name}";
            return HandleUnary(fullyQualifiedName, node.Operand);
        }

        // x++ x-- x!
        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
            if (node.IsKind(SyntaxKind.PostIncrementExpression)
                || node.IsKind(SyntaxKind.PostDecrementExpression))
                throw new System.ArgumentException("x++ and x-- should already be handled.");
            
            return base.VisitPostfixUnaryExpression(node);
        }

        SyntaxNode HandleUnary(string fullyQualifiedName, ExpressionSyntax operand) {
            return InvocationExpression(
                MemberAccessExpression(fullyQualifiedName),
                ArgumentList(
                    (ExpressionSyntax)Visit(operand)
                )
            );
        }
    }
}
