using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Turns both all property read and writes into method calls.
    /// </summary>
    public class PropertiesToMethodCallsRewriter : AbstractFullRewriter<CopyPropertiesToNamedRewriter> {

        // Attributes need no processing even though they may contain properties.
        public override SyntaxNode VisitAttribute(AttributeSyntax node) {
            return node;
        }

        // Sets of the form Property ∘= ... for any op ∘.
        // This needs to be done *before* any get-processing gets there.
        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (!TryGetPropertyInfo(node.Left, out var containingType, out var getMethodName, out var setMethodName))
                return base.VisitAssignmentExpression(node);

            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
                return InvocationExpression(
                    MemberAccessExpression(containingType, setMethodName),
                    ArgumentList((ExpressionSyntax)Visit(node.Right))
                );
            }

            // At this point, we're Property ∘= Expression.
            // Comes down to writing code in the form `Set(Get op RHS)`.
            var op = AssignmentToOperator(node.Kind());
            return InvocationExpression(
                MemberAccessExpression(containingType, setMethodName),
                ArgumentList(
                    BinaryExpression(
                        op,
                        InvocationExpression(
                            MemberAccessExpression(containingType, getMethodName)
                        ),
                        (ExpressionSyntax)base.Visit(node.Right)
                    )
                )
            );
        }

        // Remaining gets
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            if (!TryGetPropertyInfo(node, out var containingType, out var getMethodName, out var _))
                return node;

            return InvocationExpression(
                MemberAccessExpression(containingType, getMethodName)
            );
        }

        /// <summary>
        /// <para>
        /// If <paramref name="expr"/> is actually a property, returns true and
        /// outputs a bunch of useful info. Otherwise, false.
        /// </para>
        /// <para>
        /// This requires <paramref name="expr"/> to live in the original syntax
        /// tree.
        /// </para>
        /// </summary>
        public bool TryGetPropertyInfo(
            in ExpressionSyntax expr,
            out INamedTypeSymbol containingType,
            out string getMethodName,
            out string setMethodName
        ) {
            SymbolInfo info = CurrentSemantics.GetSymbolInfo(expr);
            if (info.Symbol != null && info.Symbol is IPropertySymbol property) {
                containingType = property.ContainingType;
                getMethodName = CopyPropertiesToNamedRewriter.GetGetMethodName(property.Name);
                setMethodName = CopyPropertiesToNamedRewriter.GetSetMethodName(property.Name);
                return true;
            } else {
                containingType = default;
                getMethodName = null;
                setMethodName = null;
                return false;
            }
        }

        static SyntaxKind AssignmentToOperator(SyntaxKind /*This is*/ ass) {
            return ass switch {
                SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
                SyntaxKind.SubtractAssignmentExpression => SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideAssignmentExpression => SyntaxKind.DivideExpression,
                SyntaxKind.ModuloAssignmentExpression => SyntaxKind.ModuloExpression,
                SyntaxKind.AndAssignmentExpression => SyntaxKind.LogicalAndExpression,
                SyntaxKind.OrAssignmentExpression => SyntaxKind.LogicalOrExpression,
                SyntaxKind.LeftShiftAssignmentExpression => SyntaxKind.LeftShiftExpression,
                SyntaxKind.RightShiftAssignmentExpression => SyntaxKind.RightShiftExpression,
                SyntaxKind.UnsignedRightShiftAssignmentExpression => SyntaxKind.UnsignedRightShiftExpression,
                _ => throw new ArgumentException($"Couldn't convert {ass} to an expression. Is it even a valid assignment?", nameof(ass)),
            };
        }
    }
}
