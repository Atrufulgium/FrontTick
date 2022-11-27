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

        // ..attributes exist lol. Don't process those
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
            return base.Visit(InvocationExpression(
                MethodName(containingType, methodName),
                ArgumentList(
                    node.Left,
                    node.Right
                )
            ));
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
            return base.VisitAssignmentExpression(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    node.Left,
                    InvocationExpression(
                        MethodName(containingType, methodName),
                        ArgumentList(
                            node.Left,
                            node.Right
                        )
                    )
                )
            );
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
            var op = (IUnaryOperation)CurrentSemantics.GetOperation(node);
            return HandleUnary(op, node.OperatorToken.Text, node.Operand);
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
            var op = (IUnaryOperation)CurrentSemantics.GetOperation(node);
            return HandleUnary(op, node.OperatorToken.Text, node.Operand);
        }

        SyntaxNode HandleUnary(IUnaryOperation op, string opText, ExpressionSyntax operand) {
            if (op.OperatorMethod == null)
                throw CompilationException.OperatorsRequireUnderlyingMethod;

            var containingType = op.OperatorMethod.ContainingType;
            var methodName = NameOperatorsCategory.GetMethodName(opText);
            return base.Visit(InvocationExpression(
                MethodName(containingType, methodName),
                ArgumentList(operand)
            ));
        }

        static MemberAccessExpressionSyntax MethodName(INamedTypeSymbol type, string name)
            => MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(type.Name),
                IdentifierName(name)
            );
    }
}
