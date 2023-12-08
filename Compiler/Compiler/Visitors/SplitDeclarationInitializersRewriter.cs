using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any initializer
    /// <code>
    ///     MyType t = myValue;
    /// </code>
    /// into
    /// <code>
    ///     MyType t;
    ///     t = myValue;
    /// </code>
    /// and similarly supports longer initialisations such as
    /// <code>
    ///     MyType t, u = myValue;
    ///     MyType t = myValue, y = myOtherValue;
    /// </code>
    /// </para>
    /// </summary>
    public class SplitDeclarationInitializersRewriter : AbstractFullRewriter {

        // (This code is copied @ MoveLocalDeclarationsToRootRewriter)
        // We need to put declarations at root so store them.
        readonly List<LocalDeclarationStatementSyntax> declarations = new();
        // Only true when I actually use `declarations`.
        // Used to catch (some) unimplemented declarations.
        bool wellDefined = false;

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node)
            => VisitBaseMethodDeclarationSyntax(node, base.VisitMethodDeclarationRespectingNoCompile);
        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            => VisitBaseMethodDeclarationSyntax(node, base.VisitConstructorDeclaration);
        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
            => VisitBaseMethodDeclarationSyntax(node, base.VisitOperatorDeclaration);

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

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax decl) {
            if (!wellDefined)
                throw new InvalidOperationException("Hey, self, there's a LocalDeclarationStatement in an unhandled node type.");

            var node = decl.Declaration;

            BlockSyntax ret = Block();
            var type = node.Type;
            foreach (var v in node.Variables) {
                var name = v.Identifier;
                var init = v.Initializer;

                declarations.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            type,
                            SeparatedList(
                                VariableDeclarator(name)
                            )
                        )
                    )
                );
                if (init != null) {
                    ret = ret.WithAppendedStatement(
                        SimpleAssignmentStatement(
                            IdentifierName(name),
                            init.Value
                        )
                    );
                }
            }
            return ret.SimplifyBlock();
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            // We may introduce blocks within blocks, so flatten them out.
            return ((BlockSyntax)base.VisitBlock(node)).Flattened();
        }
    }
}
