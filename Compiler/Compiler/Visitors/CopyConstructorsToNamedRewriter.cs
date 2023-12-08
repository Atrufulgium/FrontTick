using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any code inside a class <tt>T</tt> of the form
    /// <code>
    ///     T(..) { .. }
    /// </code>
    /// into code of the form
    /// <code>
    ///     T(..) { .. }
    ///     public static T -CONSTRUCT-(..) { .. }
    /// </code>
    /// with essentially the same method body, but applied to a <tt>T</tt>
    /// created at the top, and returned at the end.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This includes static constructors. Those get prefixed with the
    /// <see cref="TrueLoadAttribute"/> attribute.
    /// </remarks>
    public class CopyConstructorsToNamedRewriter : AbstractFullRewriter<ThisRewriter> {

        TypeSyntax currentType;
        
        readonly List<ConstructorDeclarationSyntax> ops = new();

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node) {
            ops.Clear();
            currentType = Type(node.Identifier.Text);
            node = (StructDeclarationSyntax)base.VisitStructDeclarationRespectingNoCompile(node);
            return AddConstructors(node);
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            ops.Clear();
            currentType = Type(node.Identifier.Text);
            node = (ClassDeclarationSyntax)base.VisitClassDeclarationRespectingNoCompile(node);
            return AddConstructors(node);
        }

        SyntaxNode AddConstructors(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();
            foreach(var op in ops) {
                // Static constructor handling and normal constructor handling is
                // quite different. Static constructors do not need their bodies
                // or arguments rewritten and are already static, but do need the
                // [TrueLoad] attribute added.
                bool isStatic = op.ChildTokensContain(SyntaxKind.StaticKeyword);

                MethodDeclarationSyntax methodDeclaration;
                if (isStatic)
                    methodDeclaration = MethodDeclaration(
                            PredefinedType(Token(SyntaxKind.VoidKeyword)),
                            Identifier("-CONSTRUCTSTATIC-")
                        ).WithAttributeLists(op.AttributeLists)
                         .WithAddedAttribute(MCMirrorTypes.TrueLoadAttribute)
                         .WithModifiers(op.Modifiers)
                         .WithBody(op.Body)
                         .WithParameterList(op.ParameterList); // should be empty, but if not, throws later
                else
                    methodDeclaration =
                        MethodDeclaration(
                            currentType,
                            Identifier("-CONSTRUCT-")
                        ).WithAttributeLists(op.AttributeLists)
                         .WithModifiers(op.Modifiers.Add(Token(SyntaxKind.StaticKeyword)))
                         .WithBody((BlockSyntax) new ConstructorRewriter(currentType).Visit(op.Body))
                         .WithParameterList(op.ParameterList);

                newMethods.Add(methodDeclaration);
            }
            node = node.AddMembers(newMethods.ToArray());
            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
            ops.Add(node);
            return base.VisitConstructorDeclaration(node);
        }

        /// <summary>
        /// Rewrite a constructor body
        /// <code>
        ///     T(args) {
        ///         body
        ///     }
        /// </code>
        /// into a method body
        /// <code>
        ///     T CONSTRUCT-T(args) {
        ///         T #new = default;
        ///         body where `this.` => `#new.`
        ///         return T;
        ///     }
        /// </code>
        /// </summary>
        private class ConstructorRewriter : CSharpSyntaxRewriter {

            // Writing directly to the return value in constructors is
            // preferable. Nothing happens in them.
            static readonly string varName = "#RET";
            readonly TypeSyntax type;
            int depth = 0;

            public ConstructorRewriter(TypeSyntax type) => this.type = type;

            public override SyntaxNode VisitBlock(BlockSyntax node) {
                // Of course only mutate the outermost block when adding the
                // new first and last lines.
                if (depth == 0) {
                    // TODO: not initializing anything is correct with structs. Not with classes.
                    node = node.WithPrependedStatement(
                        LocalDeclarationStatement(type, varName)
                    );
                    node = node.WithAppendedStatement(
                        ReturnStatement(
                            IdentifierName(varName)
                        )
                    );
                }
                depth++;
                var ret = base.VisitBlock(node);
                depth--;
                return ret;
            }

            public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
                => IdentifierName(varName);
        }
    }
}
