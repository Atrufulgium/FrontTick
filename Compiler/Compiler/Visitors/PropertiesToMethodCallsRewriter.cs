using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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
            // TODO: Property assignment can also be done as `+= OtherClass.Property`, which fails here.
            if (!TryGetPropertyInfo(node.Left, out var containingType, out var getMethodName, out var setMethodName, out bool isStatic))
                return base.VisitAssignmentExpression(node);

            ExpressionSyntax setInvocation = isStatic
                ? MemberAccessExpression(containingType, setMethodName)
                : IdentifierName(setMethodName);

            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
                return InvocationExpression(
                    setInvocation,
                    ArgumentList((ExpressionSyntax)Visit(node.Right))
                );
            }

            ExpressionSyntax getInvocation = isStatic
                ? MemberAccessExpression(containingType, getMethodName)
                : IdentifierName(getMethodName);

            // At this point, we're Property ∘= Expression.
            // Comes down to writing code in the form `Set(Get op RHS)`.
            var op = AssignmentToOperator(node.Kind());

            return InvocationExpression(
                setInvocation,
                ArgumentList(
                    BinaryExpression(
                        op,
                        InvocationExpression(
                            getInvocation
                        ),
                        (ExpressionSyntax)base.Visit(node.Right)
                    )
                )
            );
        }

        // Remaining gets
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            if (!TryGetPropertyInfo(node, out var containingType, out var getMethodName, out var _, out bool isStatic))
                return base.VisitIdentifierName(node);

            ExpressionSyntax getInvocation = isStatic
                ? MemberAccessExpression(containingType, getMethodName)
                : IdentifierName(getMethodName);

            return InvocationExpression(getInvocation);
        }

        // Roslyn doesn't like Identifier.[Invocation()], only [Identifier.Invocation]().
        // Manually walk the tree if we would create the former case.
        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
            // Note to self: `node` represents "[Expression].[Name]". I don't like this naming.
            if (node.Name is IdentifierNameSyntax identifier) {
                var result = VisitIdentifierName(identifier);
                if (result is InvocationExpressionSyntax call) {
                    // This is the actual case where we would have Identifier.[Invocation()] if
                    // we simply updated `Name` to the result.
                    // Change [node].[Invocation()] to [node.Invocation]().

                    // Had [node.Expression].[identifier] => [node.Expression].[simpleName()]
                    // Turn into [[node.Expression].simpleName]().
                    if (call.Expression is SimpleNameSyntax simpleName)
                        return call.WithExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                node.Expression,
                                simpleName
                            )
                        );

                    // Had [node.Expression].[identifier] => [node.Expression].[[expression.name]()]
                    // Turn into [[[node.Expression].[expression]].name]()
                    // Take into account the expression itself may also be a node.
                    // I.E. just flip the tree structure.
                    if (call.Expression is MemberAccessExpressionSyntax access) {
                        Stack<SimpleNameSyntax> names = new();
                        ExpressionSyntax expression;
                        while (true) {
                            expression = access.Expression;
                            names.Push(access.Name);
                            if (expression is MemberAccessExpressionSyntax newAccess) {
                                access = newAccess;
                                continue;
                            }
                            break;
                        }
                        // We have, in order, all names in `names`, and a
                        // single remaining non-access expression.
                        ExpressionSyntax accessPile = expression;
                        while (names.Count > 0) {
                            accessPile = MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                accessPile,
                                names.Pop()
                            );
                        }
                        return call.WithExpression(accessPile);
                    }
                    throw new NotImplementedException("What case reaches here?");
                }
                return node.WithName((IdentifierNameSyntax) result);
            }
            return base.VisitMemberAccessExpression(node);
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
            out string setMethodName,
            out bool isStatic
        ) {
            SymbolInfo info = CurrentSemantics.GetSymbolInfo(expr);
            if (info.Symbol != null && info.Symbol is IPropertySymbol property) {
                containingType = property.ContainingType;
                getMethodName = CopyPropertiesToNamedRewriter.GetGetMethodName(property.Name);
                setMethodName = CopyPropertiesToNamedRewriter.GetSetMethodName(property.Name);
                isStatic = property.IsStatic;
                return true;
            } else {
                containingType = default;
                getMethodName = null;
                setMethodName = null;
                isStatic = false;
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
