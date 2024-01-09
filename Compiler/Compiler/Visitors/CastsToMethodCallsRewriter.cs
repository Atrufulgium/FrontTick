using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns both explicit <tt>(B)a</tt> into <tt>#CAST#...(a)</tt> and
    /// implicit <tt>a</tt> that become B into <tt>#CAST#...(a)</tt>.
    /// </para>
    /// </summary>
    // Exception: Implicit cast attempts towards "object" are ignored.
    // This is because a bunch of compiler intrinsics accept object as a
    // catch-all.
    public class CastsToMethodCallsRewriter : AbstractFullRewriter<CopyCastsToNamedRewriter> {

        CopyCastsToNamedRewriter CopyCastsToNamedRewriter => Dependency1;

        // Explicit casts
        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node) {
            var toType = CurrentSemantics.GetTypeInfo(node.Type).Type;
            var fromType = CurrentSemantics.GetTypeInfo(node.Expression).Type;
            var (type, name) = CopyCastsToNamedRewriter.GetMethodName(fromType, toType);
            return InvocationExpression(
                MemberAccessExpression(type + "." + name),
                ArgumentList(
                    (ExpressionSyntax)Visit(node.Expression)    
                )
            );
        }

        // Implicit casts
        // (Note that visiting IdentifierNameSyntaxes covers *a lot more*.)
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            var toType = CurrentSemantics.GetTypeInfo(node).ConvertedType;
            if (toType == null)
                return base.VisitIdentifierName(node);

            // Our lovely exception.
            if (CurrentSemantics.TypesMatch(toType, MCMirrorTypes.Object))
                return base.VisitIdentifierName(node);

            var fromType = CurrentSemantics.GetTypeInfo(node).Type;
            // Is there even an implicit cast?
            if (CurrentSemantics.TypesMatch(toType, fromType)) {
                return base.VisitIdentifierName(node);
            }

            var (type, name) = CopyCastsToNamedRewriter.GetMethodName(fromType, toType);
            return InvocationExpression(
                MemberAccessExpression(type + "." + name),
                ArgumentList(
                    (ExpressionSyntax)base.VisitIdentifierName(node)
                )
            );
        }
    }
}
