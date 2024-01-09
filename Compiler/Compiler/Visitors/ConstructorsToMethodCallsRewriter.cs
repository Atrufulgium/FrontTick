using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns <tt>new(..);</tt> and <tt>new T(..)</tt> into
    /// <tt>T.-CONSTRUCT-(..)</tt>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The initializers { .. } are not supported and should be done in a
    /// previous stage.
    /// </remarks>
    // For structs we get away with not using a <tt>new()</tt>, but classes
    // will require it. In that case, we will need to filter out the trivial
    // news from all nontrivial ones.
    public class ConstructorsToMethodCallsRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            => VisitBaseObjectCreationExpression(node, base.VisitObjectCreationExpression);

        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
            => VisitBaseObjectCreationExpression(node, base.VisitImplicitObjectCreationExpression);

        SyntaxNode VisitBaseObjectCreationExpression<T>(T node, Func<T, SyntaxNode> basecall) where T : BaseObjectCreationExpressionSyntax {
            if (node.Initializer != null)
                AddCustomDiagnostic(DiagnosticRules.Unsupported, node.GetLocation(), "initializers", "Low priority.");

            var methodSymbol = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(node).Symbol;
            var typeSymbol = methodSymbol.ContainingType;
            var typeSymbolName = CurrentSemantics.GetFullyQualifiedNameIncludingPrimitives(typeSymbol);

            node = (T)basecall(node);

            // ContainingType instead of ReturnType because constructors are void.
            return InvocationExpression(
                MemberAccessExpression(typeSymbolName + ".-CONSTRUCT-"),
                node.ArgumentList
            );
        }
    }
}
