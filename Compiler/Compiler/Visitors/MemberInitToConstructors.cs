using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Given classes/structs with members <tt>field = value;</tt>, moves
    /// those <tt>= value</tt> assignments into every constructor.
    /// </para>
    /// <para>
    /// This is including the argument-less constructor, which gets created if
    /// it does not yet exist. This is <i>also</i> including the static
    /// constructor for static member initializers.
    /// </para>
    /// <para>
    /// It also sets non-initialized members to default.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Assumes multiple declarators (like <tt>int a,b</tt>) are no more.
    /// </para>
    /// </remarks>
    // (This also handles auto-properties if their backing field is implemented
    //  before this pass)
    public class MemberInitToConstructors : AbstractFullRewriter {

        readonly List<MemberDeclarationSyntax> newMembers = new();
        readonly List<StatementSyntax> constructorAssignments = new();
        readonly List<StatementSyntax> staticConstructorAssignments = new();

        // Want specifically the current type name and NOT any qualification.
        // That's why we're doing `.Name` below.
        string currentTypeName;

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node) {
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).Name;
            HandleMembers(node.Members);
            return node.WithMembers(List(newMembers));
            
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).Name;
            HandleMembers(node.Members);
            return node.WithMembers(List(newMembers));
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
            return node.WithBody(node.Body.WithPrependedStatement(constructorAssignments));
        }

        /// <summary>
        /// <para>
        /// Goes through all members and puts no-init versions into the
        /// <see cref="newMembers"/> list. All to-copy initializers end up in
        /// <see cref="constructorAssignments"/>.
        /// </para>
        /// <para>
        /// Const members are ignored.
        /// </para>
        /// </summary>
        void HandleMembers(IEnumerable<MemberDeclarationSyntax> members) {
            newMembers.Clear();
            constructorAssignments.Clear();
            staticConstructorAssignments.Clear();

            bool foundEmptyConstructor = false; // T() with no args
            bool foundStaticConstructor = false; // static T() (always no args)

            foreach (var m in members) {
                if (!foundEmptyConstructor && m is ConstructorDeclarationSyntax c) {
                    if (c.ParameterList == null || c.ParameterList.Parameters.Count == 0) {
                        if (c.ChildTokensContain(SyntaxKind.StaticKeyword))
                            foundStaticConstructor = true;
                        else
                            foundEmptyConstructor = true;
                    }
                }
                if (m is not FieldDeclarationSyntax f) {
                    newMembers.Add(m);
                    continue;
                }
                if (f.ChildTokensContain(SyntaxKind.ConstKeyword)) {
                    newMembers.Add(m);
                    continue;
                }

                // We're a variable
                var declaration = f.Declaration;
                var variable = declaration.Variables.First();
                var noInit = SingletonSeparatedList(variable.WithInitializer(null));
                newMembers.Add(f.WithDeclaration(declaration.WithVariables(noInit)));

                // If this variable has an initializer, add it to the constructor list.
                // Otherwise, make the initializer the default initializer.
                var init = variable.Initializer;
                init ??= EqualsValueClause(
                    LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                );
                bool isStatic = f.ChildTokensContain(SyntaxKind.StaticKeyword);
                var rhs = init.Value;
                if (isStatic)
                    staticConstructorAssignments.Add(
                        AssignmentStatement(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(variable.Identifier.Text),
                            rhs
                        )
                    );
                else
                    constructorAssignments.Add(
                        AssignmentStatement(
                            SyntaxKind.SimpleAssignmentExpression,
                            ThisAccessExpression(variable.Identifier.Text),
                            rhs
                        )
                    );
            }

            // Note: empty struct constructors must be public, so just to be
            // safe, add the public keyword.

            if (!foundEmptyConstructor && constructorAssignments.Count > 0)
                newMembers.Add(
                    ConstructorDeclaration(Identifier(currentTypeName))
                    .WithBody(Block())
                    .WithAddedModifier(SyntaxKind.PublicKeyword)
                );
            if (!foundStaticConstructor && staticConstructorAssignments.Count > 0)
                newMembers.Add(
                    ConstructorDeclaration(Identifier(currentTypeName))
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                    .WithBody(Block())
                );

            // Now go through the new members again, and update all
            // constructors to have the prepended statements.
            for (int i = 0; i < newMembers.Count; i++) {
                if (newMembers[i] is ConstructorDeclarationSyntax c) {
                    if (c.ChildTokensContain(SyntaxKind.StaticKeyword))
                        newMembers[i] = c.WithBody(c.Body.WithPrependedStatement(staticConstructorAssignments));
                    else
                        newMembers[i] = c.WithBody(c.Body.WithPrependedStatement(constructorAssignments));
                }
            }
        }
    }
}
