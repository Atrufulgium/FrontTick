using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Removes any property member.
    /// </summary>
    public class RemovePropertiesRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) => null;
    }
}
