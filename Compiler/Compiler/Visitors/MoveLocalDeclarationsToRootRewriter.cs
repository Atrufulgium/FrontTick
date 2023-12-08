using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// A rewriter (intended pretty late into the process when only methods are
    /// remaining) that pulls any method-local declaration to its root scope.
    /// </summary>
    public class MoveLocalDeclarationsToRootRewriter : AbstractFullRewriter {

        // (This code is copied @ SplitLocalDeclarationsRewriter)
        // We need to put declarations at root so store them.
        readonly List<LocalDeclarationStatementSyntax> declarations = new();
        // Only true when I actually use `declarations`.
        // Used to catch (some) unimplemented declarations.
        bool wellDefined = false;

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node)
            => VisitBaseMethodDeclarationSyntax(node, base.VisitMethodDeclarationRespectingNoCompile);

        SyntaxNode VisitBaseMethodDeclarationSyntax<T>(T node, Func<T, SyntaxNode> baseVisit) where T : BaseMethodDeclarationSyntax {
            declarations.Clear();

            wellDefined = true;
            node = (T)baseVisit(node);
            wellDefined = false;
            if (node.Body == null)
                return node;

            var body = node.Body;
            body = body.WithPrependedStatement(declarations);
            return node.WithBody(body);
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) {
            if (!wellDefined)
                throw new InvalidOperationException("Hey, self, there's a LocalDeclarationStatement in an unhandled node type.");

            declarations.Add(node);
            return null;
        }
    }
}
