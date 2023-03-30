using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes any method that is an implicit/explicit cast.
    /// </para>
    /// </summary>
    public class RemoveCastRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
            => null;
    }
}
