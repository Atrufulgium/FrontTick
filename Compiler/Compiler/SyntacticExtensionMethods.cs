using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Extension methods relating to everything not needing a semantic
    /// interpretation of symbols' meaning.
    /// </summary>
    public static class SyntacticExtensionMethods {

        public static bool ChildTokensContain(this SyntaxNode node, SyntaxKind token)
            => (from t in node.ChildTokens() where t.IsKind(token) select t).Any();

        /// <summary>
        /// Whether or not this method is of void signature.
        /// </summary>
        public static bool ReturnsVoid(this MethodDeclarationSyntax method)
            => method.ReturnType.ChildTokensContain(SyntaxKind.VoidKeyword);

        /// <summary>
        /// Whether or not a method definition is extern.
        /// </summary>
        public static bool IsExtern(this BaseMethodDeclarationSyntax method)
            => method.ChildTokensContain(SyntaxKind.ExternKeyword);

        /// <summary>
        /// The property <see cref="MethodDeclarationSyntax.Arity"/> is used to
        /// get the number of generic type parameters (resulting in 0 for non-
        /// generic methods). This method gives the *actual* arity: the number
        /// of arguments of a method.
        /// </summary>
        /// <remarks>
        /// Note that this method is dumb -- all arguments count as 1 argument,
        /// including optional arguments and <c>params</c> arguments.
        /// </remarks>
        // This unconventional name instead of something like "GetArgumentCount"
        // to make it appear *right below* `Arity` in all autocompletes.
        public static int ArityOfArguments(this MethodDeclarationSyntax node)
            => node.ParameterList.Parameters.Count;

        /// <summary>
        /// Extracts the target label from a goto statement, assuming it is a
        /// regular goto not belonging to `case`.
        /// </summary>
        public static string Target(this GotoStatementSyntax got)
            => got.Kind() == SyntaxKind.GotoStatement
                ? ((IdentifierNameSyntax)got.Expression).Identifier.Text
                : throw new System.ArgumentException("This method only works for non-case gotos.", nameof(got));

        /// <summary>
        /// Returns a new BlockSyntax node that is equal to <paramref name="block"/>
        /// except for all statements in <paramref name="statement"/> being
        /// appended to after the statement list.
        /// </summary>
        /// <remarks>
        /// (Yes, <see cref="BlockSyntax.AddStatements(StatementSyntax[])"/>
        ///  exists, but is not obvious in *where* it adds them.)
        /// </remarks>
        public static BlockSyntax WithAppendedStatement(this BlockSyntax block, IEnumerable<StatementSyntax> statement)
            => SyntaxFactory.Block(block.Statements.Concat(statement));

        /// <inheritdoc cref="WithAppendedStatement(BlockSyntax, IEnumerable{StatementSyntax})"/>
        public static BlockSyntax WithAppendedStatement(this BlockSyntax block, params StatementSyntax[] statement)
            => block.WithAppendedStatement((IEnumerable<StatementSyntax>)statement);

        /// <inheritdoc cref="WithAppendedStatement(BlockSyntax, IEnumerable{StatementSyntax})"/>
        public static BlockSyntax WithAppendedStatement(this BlockSyntax block, StatementSyntax statement)
            => block.WithAppendedStatement(new[] { statement });

        /// <summary>
        /// Returns a new BlockSyntax node that is equal to <paramref name="block"/>
        /// except for all statements in <paramref name="statement"/> being
        /// prepended to before the statement list.
        /// </summary>
        public static BlockSyntax WithPrependedStatement(this BlockSyntax block, IEnumerable<StatementSyntax> statement)
            => SyntaxFactory.Block(statement.Concat(block.Statements));

        /// <inheritdoc cref="WithPrependedStatement(BlockSyntax, IEnumerable{StatementSyntax})"/>
        public static BlockSyntax WithPrependedStatement(this BlockSyntax block, params StatementSyntax[] statement)
            => block.WithPrependedStatement((IEnumerable<StatementSyntax>)statement);

        /// <inheritdoc cref="WithPrependedStatement(BlockSyntax, IEnumerable{StatementSyntax})"/>
        public static BlockSyntax WithPrependedStatement(this BlockSyntax block, StatementSyntax statement)
            => block.WithPrependedStatement(new[] { statement } );

        /// <summary>
        /// <para>
        /// Turns block statements that have nested block statements into a
        /// single list of statements without nested block statements.
        /// </para>
        /// <para>
        /// Blocks part of labels count as seperate from their parent blocks.
        /// </para>
        /// </summary>
        public static BlockSyntax Flattened(this BlockSyntax block) {
            List<BlockSyntax> nestedNodes = new();
            // Add all direct blocks and blocks that are labeled.
            foreach (var s in block.Statements) {
                var statement = s;
                if (statement is BlockSyntax b)
                    nestedNodes.Add(b);
            }

            // Create a new block by replacing everything with its flattening.
            foreach (var b in nestedNodes) {
                block = block.ReplaceNode(b, b.Flattened().Statements);
            }
            return block;
        }

        /// <summary>
        /// Ignoring the goto's kind, returns the associated literal.
        /// </summary>
        public static string Identifier(this GotoStatementSyntax got)
            => ((IdentifierNameSyntax)got.Expression).Identifier.Text;

        /// <summary>
        /// <para>
        /// Whether this node is one of `return`, `goto`, `break`, or
        /// `continue`, or whether this contains blocks that *all* end in
        /// such statement.
        /// </para>
        /// <para>
        /// This assumes <see cref="Visitors.GuaranteeBlockRewriter"/>.
        /// </para>
        /// </summary>
        public static bool AllPathsJump(this SyntaxNode node) {
            // I hate this pile of edge cases.
            if (node is ReturnStatementSyntax or GotoStatementSyntax
                or BreakStatementSyntax or ContinueStatementSyntax)
                return true; // We're a single jump
            else if (node.DescendantNodes().OfType<BlockSyntax>().IsEmpty())
                return false; // We're not even branching

            foreach (var descendant in node.DescendantNodesAndSelf()) {
                if (descendant is BlockSyntax block) {
                    if (block.Statements.Count == 0)
                        return false; // Of course a non-jump if empty
                    if (block.Statements.Last() is not 
                        (ReturnStatementSyntax or GotoStatementSyntax
                        or BreakStatementSyntax or ContinueStatementSyntax))
                        return false; // Some branch ends in non-jump.
                }
                if (descendant is IfStatementSyntax ifst
                    && (ifst.Statement is not BlockSyntax || ifst.Else == null
                    || ifst.Else.Statement is not BlockSyntax))
                    return false; // Else must exist
            }
            return true; // All branches end in jump
        }

        /// <summary>
        /// Gets a list of sibling nodes (including itself) in prefix document order.
        /// </summary>
        public static IEnumerable<SyntaxNode> SiblingNodesAndSelf(this SyntaxNode node) {
            if (node.Parent == null)
                return new[] { node };
            return node.Parent.ChildNodes();
        }

        /// <summary>
        /// Gets a list of sibling nodes in prefix document order.
        /// </summary>
        public static IEnumerable<SyntaxNode> SiblingNodes(this SyntaxNode node) {
            return node.SiblingNodesAndSelf().Skip(node);
        }

        /// <summary>
        /// <para>
        /// Returns the next sibling according to prefix document order.
        /// </para>
        /// <para>
        /// Will be <tt>null</tt> if the next sibling does not exist.
        /// </para>
        /// </summary>
        public static SyntaxNode NextSibling(this SyntaxNode node) {
            bool foundNode = false;
            foreach (var sibling in node.SiblingNodesAndSelf()) {
                if (foundNode)
                    return sibling;
                foundNode = sibling.Equals(node);
            }
            return null;
        }

        /// <summary>
        /// <para>
        /// Returns the previous sibling according to prefix document order.
        /// </para>
        /// <para>
        /// Will be <tt>null</tt> if the previous sibling does not exist.
        /// </para>
        /// </summary>
        public static SyntaxNode PrevSibling(this SyntaxNode node) {
            SyntaxNode prevNode = null;
            foreach (var sibling in node.SiblingNodesAndSelf()) {
                if (sibling.Equals(node))
                    return prevNode;
                prevNode = sibling;
            }
            return null;
        }

        /// <summary>
        /// Replaces the last statement of a block with the given statement.
        /// Throws when there's no statements.
        /// </summary>
        public static BlockSyntax WithLastStatementReplaced(this BlockSyntax block, StatementSyntax statementSyntax) {
            var statements = block.Statements;
            statements = statements.RemoveAt(statements.Count - 1);
            statements = statements.Add(statementSyntax);
            return block.WithStatements(statements);
        }

        /// <summary>
        /// If a block ends with
        /// <code>
        ///         // Stuff
        ///         label: {
        ///             // Stuff
        ///             label: {
        ///                 // Stuff
        ///                 ...
        ///                     label {
        ///                         // Stuff
        ///                     }
        ///                 ...
        ///             }
        ///         }
        /// </code>
        /// this adds <paramref name="block"/> to the end of the innermost label.
        /// </summary>
        public static BlockSyntax WithAppendedStatementThroughLabels(this BlockSyntax block, StatementSyntax statement) {
            // Collect all relevant scopes
            Stack<BlockSyntax> finalBlocks = new();
            finalBlocks.Push(block);
            while (block.Statements.Count > 0 &&
                block.Statements.Last() is LabeledStatementSyntax lab) {
                block = (BlockSyntax)lab.Statement;
                finalBlocks.Push(block);
            }

            // Specially handle the top scope.
            block = finalBlocks.Pop();
            block = block.WithAppendedStatement(statement);

            // For each block, replace the label to point to the modified block.
            while (finalBlocks.Count > 0) {
                var outerBlock = finalBlocks.Pop();
                var lastLabel = (LabeledStatementSyntax)outerBlock.Statements.Last();
                lastLabel = lastLabel.WithStatement(block);
                outerBlock = outerBlock.WithLastStatementReplaced(lastLabel);
                block = outerBlock;
            }

            return block;
        }

        public static bool IsImplicitConversion(this ConversionOperatorDeclarationSyntax op)
            => op.ImplicitOrExplicitKeyword.Text == "implicit";

        // no i don't like the name this way it pops up in autocomplete and I don't fuck up
        public static T WithAdditionalMembers<T>(this TypeDeclarationSyntax declaration, params MemberDeclarationSyntax[] members) where T : TypeDeclarationSyntax
            => (T) declaration.AddMembers(members);

        public static T WithAdditionalMembers<T>(this TypeDeclarationSyntax declaration, IEnumerable<MemberDeclarationSyntax> members) where T : TypeDeclarationSyntax
            => (T)declaration.AddMembers(members.ToArray());

        public static ParameterListSyntax WithPrependedArguments(this ParameterListSyntax list, params ParameterSyntax[] items)
            => list.WithParameters(list.Parameters.InsertRange(0, items));

        public static ArgumentListSyntax WithPrependedArguments(this ArgumentListSyntax list, params ArgumentSyntax[] items)
            => list.WithArguments(list.Arguments.InsertRange(0, items));

        public static MethodDeclarationSyntax WithAddedAttribute(this MethodDeclarationSyntax method, AttributeSyntax attribute) {
            var attributeLists = method.AttributeLists;
            attributeLists = attributeLists.Add(AttributeList(SeparatedList(new[] { attribute } )));
            return method.WithAttributeLists(attributeLists);
        }

        /// <summary>
        /// Tries to add an attribute to the method. Note that this can easily
        /// fail if the argument attribute does not exist in the compilation,
        /// or if the overload is not found. There is no checking whether
        /// everything makes sense.
        /// </summary>
        public static MethodDeclarationSyntax WithAddedAttribute<T>(this MethodDeclarationSyntax method) where T : Attribute
            => method.WithAddedAttribute(Attribute(QualifiedName(typeof(T).FullName.Replace("Attribute", ""))));

        /// <inheritdoc cref="WithAddedAttribute{T}(MethodDeclarationSyntax)"/>
        public static MethodDeclarationSyntax WithAddedAttribute<T>(this MethodDeclarationSyntax method, params object[] constructorArgs) {
            List<AttributeArgumentSyntax> args = new(constructorArgs.Length);
            for (int i = 0; i < constructorArgs.Length; i++) {
                var param = constructorArgs[i];
                ExpressionSyntax expr = null;
                if (param is string str)
                    expr = StringLiteralExpression(str);
                else if (param is int ii)
                    expr = NumericLiteralExpression(ii);
                else
                    throw new ArgumentException("Given constructor argument is not a supported type. Be sure not to pass in syntax nodes, but raw values.", nameof(constructorArgs));
                args.Add(AttributeArgument(expr));
            }
            return method.WithAddedAttribute(
                Attribute(
                    QualifiedName(typeof(T).FullName),
                    AttributeArgumentList(SeparatedList(args))
                )
            );
        }
    }
}
