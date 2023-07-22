using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any instance access without <tt>this</tt> into one with <tt>this</tt>.
    /// </para>
    /// </summary>
    public class ThisRewriter : AbstractFullRewriter<ArrowRewriter> {

        int depth = 0;

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            depth++;
            var ret = base.VisitBlock(node);
            depth--;
            return ret;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            // Don't do anything if not in method (detect by being inside any block
            // expression).
            // This may not work for arrow expressions, but those are handled
            // already.
            if (depth == 0)
                return base.VisitIdentifierName(node);
            // If this is already part of an accessor, then it won't need anything.
            if (node.Parent is MemberAccessExpressionSyntax)
                return base.VisitIdentifierName(node);
            var symbol = CurrentSemantics.GetSymbolInfo(node).Symbol;
            // We only want to rewrite nonstatic locals of course.
            if (symbol.IsStatic)
                return base.VisitIdentifierName(node);
            // Some other cases that don't need `this`
            if (symbol is ILabelSymbol or ILocalSymbol or IParameterSymbol or ITypeSymbol)
                return base.VisitIdentifierName(node);

            if (symbol is not (IFieldSymbol or IMethodSymbol or IPropertySymbol))
                throw new System.NotImplementedException("I don't know what case would create this, poke me.");

            var baseName = base.VisitIdentifierName(node);
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                (IdentifierNameSyntax)baseName
            );
        }
    }
}
