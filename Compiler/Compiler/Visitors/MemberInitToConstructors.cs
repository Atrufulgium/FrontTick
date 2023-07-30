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
    /// it does not yet exist.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Assumes multiple declarators (like <tt>int a,b</tt>) are no more.
    /// </para>
    /// </remarks>
    // (This also handles auto-properties if their backing field is implemented
    //  before this pass)
    // TODO: Classes should be inited to default even if there is no init in
    // the definition. Structs don't need to due to CS0171.
    public class MemberInitToConstructors : AbstractFullRewriter {

        readonly List<MemberDeclarationSyntax> newMembers = new();
        readonly List<StatementSyntax> constructorAssignments = new();

        string currentTypeName;

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node) {
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            HandleMembers(node.Members);
            return node.WithMembers(List(newMembers));
            
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
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

            bool foundEmptyConstructor = false;

            foreach (var m in members) {
                if (!foundEmptyConstructor && m is ConstructorDeclarationSyntax c) {
                    if (c.ParameterList == null || c.ParameterList.Parameters.Count == 0)
                        foundEmptyConstructor = true;
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
                var init = variable.Initializer;
                if (init != null) {
                    var rhs = init.Value;
                    constructorAssignments.Add(
                        AssignmentStatement(
                            SyntaxKind.SimpleAssignmentExpression,
                            ThisAccessExpression(variable.Identifier.Text),
                            rhs
                        )
                    );
                }
            }

            if (!foundEmptyConstructor && constructorAssignments.Count > 0)
                newMembers.Add(ConstructorDeclaration(Identifier(currentTypeName)));

            // Now go through the new members again, and update all
            // constructors to have the prepended statements.
            for (int i = 0; i < newMembers.Count; i++) {
                if (newMembers[i] is ConstructorDeclarationSyntax c) {
                    newMembers[i] = c.WithBody(c.Body.WithPrependedStatement(constructorAssignments));
                }
            }
        }
    }
}
