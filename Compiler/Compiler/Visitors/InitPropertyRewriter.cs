using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This rewriter turns <tt>init;</tt> properties into <tt>set;</tt>
    /// properties.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note that after c#'s accessibility checks, these are equivalent.
    /// </remarks>
    public class InitPropertyRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            var accessorList = node.AccessorList.Accessors;
            List <AccessorDeclarationSyntax> newAccessors = new(accessorList.Count);
            foreach (var accessor in node.AccessorList.Accessors) {
                if (accessor.Keyword.IsKind(SyntaxKind.InitKeyword))
                    newAccessors.Add(accessor.WithKeyword(Token(SyntaxKind.SetKeyword)));
                else
                    newAccessors.Add(accessor);
            }

            var newAccessorList = node.AccessorList.WithAccessors(new(newAccessors));
            return node.WithAccessorList(newAccessorList);
        }
    }
}
