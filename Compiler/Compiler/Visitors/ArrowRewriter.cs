using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This rewriter turns code of the form
    /// <code>
    ///     => expression;
    /// </code>
    /// into code of the form
    /// <code>
    ///     {
    ///         return expression;
    ///     }
    /// </code>
    /// or
    /// <code>
    ///     {
    ///         expression;
    ///     }
    /// </code>
    /// depending on which is applicable.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is not yet implemented for indexers.
    /// </remarks>
    // In actuality it is more thorough than the above, as a block can't be cast
    // to be a method body unfortunately.

    // Arrows can appear in the following contexts. Some of these are implemented,
    // some aren't. Shrug.
    // REPLACING BLOCKS:
    // * constructor_declaration
    // * conversion_operator_declaration
    // * destructor_declaration
    // * method_declaration
    // * operator_declaration
    // * accessor_declaration
    // * local_function_statement
    // REPLACING "accessor_list" ~ a list of multiple "accessor_declaration"s.
    // This replacement makes it function only as the "get" part.
    // * indexer_declaration
    // * property_declaration
    // The replacement will either be a block of { return expression; } or just
    // { expression } if the return is void.
    public class ArrowRewriter : AbstractFullRewriter {

        // Everything but properties and indexers are handled exactly the same way.
        // If there's an arrow, simply turn it into a block.
        // Since `.WithBody` and `.WithExpressionBody` are things, and a `.WithBody`
        // of expression type doesn't get cast to one having `.WithExpressionBody`,
        // this is awkward.
        SyntaxNode VisitEasyCase<T>(T node, Func<T, SyntaxNode> baseCall, bool isVoid)
            where T : BaseMethodDeclarationSyntax
            => node.Body == null && !node.IsExtern()
            ? node.WithBody(
                ArrowToBlock(
                    node.ExpressionBody,
                    isVoid
                )
            ).WithExpressionBody(default)
            : baseCall(node);

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            => VisitEasyCase(node, base.VisitConstructorDeclaration, isVoid: false); // Imagine if constructors didn't return anything
        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
            => VisitEasyCase(node, base.VisitConversionOperatorDeclaration, isVoid: false); // Nonvoid by CS0590
        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
            => VisitEasyCase(node, base.VisitDestructorDeclaration, isVoid: true); // These aren't even supported
        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node)
            => VisitEasyCase(node, base.VisitMethodDeclarationRespectingNoCompile, isVoid: node.ReturnsVoid());
        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
            => VisitEasyCase(node, base.VisitOperatorDeclaration, isVoid: false); // Nonvoid by CS0590

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            // This can be one of three things:
            //   int I => i;
            //   int I => { get => i; set => i = value; }
            //   int I => { get { return i; } set { i = value; } }
            // Update the first two to become the third. (Omit skipped get/sets.)
            // Note: we're not upgrading the first to the second to the third,
            // but directly first to third as "ArrowToBlock" requires its
            // argument to be in the original tree.
            if (node.ExpressionBody != null) {
                return node.WithAccessorList(
                    AccessorList(
                        new SyntaxList<AccessorDeclarationSyntax>(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(
                                ArrowToBlock(node.ExpressionBody, false) // Getter isn't void
                            )
                            .WithExpressionBody(null)
                        )
                    )
                );
            }
            // We are now guaranteed to have an AccessorList.
            var accessors = node.AccessorList.Accessors;
            var newAccessors = new List<AccessorDeclarationSyntax>(accessors.Count);
            foreach (var a in accessors) {
                var accessor = a;
                bool isVoid = accessor.Keyword.IsKind(SyntaxKind.SetKeyword); // Setters *are* void
                if (accessor.ExpressionBody != null) {
                    accessor = accessor.WithBody(ArrowToBlock(accessor.ExpressionBody, isVoid))
                                       .WithExpressionBody(null);
                }
                newAccessors.Add(accessor);
            }
            return node.WithAccessorList(
                node.AccessorList.WithAccessors(new(newAccessors))
            );
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
            => throw new NotImplementedException();

        /// <summary>
        /// <para>
        /// Converts an arrow statement into an appropriate block statement to
        /// function as replacement. This requires <paramref name="node"/> to
        /// be in the syntax tree.
        /// </para>
        /// <para>
        /// Also immediately visits the generated block.
        /// </para>
        /// </summary>
        BlockSyntax ArrowToBlock(ArrowExpressionClauseSyntax node, bool isVoid) {
            if (isVoid) {
                // A setter
                return (BlockSyntax)Visit(
                    Block(
                        ExpressionStatement(node.Expression)
                    )
                );
            } else {
                // A getter
                return (BlockSyntax)Visit(
                    Block(
                        ReturnStatement(node.Expression)
                    )
                );
            }
        }
    }
}
