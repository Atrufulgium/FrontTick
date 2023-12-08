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
