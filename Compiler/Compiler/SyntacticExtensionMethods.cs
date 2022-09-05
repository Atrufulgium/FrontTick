using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

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
        public static BlockSyntax WithAppendedStatement(this BlockSyntax block, IEnumerable<StatementSyntax> statement) {
            return SyntaxFactory.Block(block.Statements.Concat(statement));
        }

        /// <inheritdoc/>
        public static BlockSyntax WithAppendedStatement(this BlockSyntax block, StatementSyntax statement)
            => block.WithAppendedStatement(new[] { statement });

        /// <summary>
        /// Returns a new BlockSyntax node that is equal to <paramref name="block"/>
        /// except for all statements in <paramref name="statement"/> being
        /// prepended to before the statement list.
        /// </summary>
        public static BlockSyntax WithPrependedStatement(this BlockSyntax block, IEnumerable<StatementSyntax> statement) {
            return SyntaxFactory.Block(statement.Concat(block.Statements));
        }

        /// <inheritdoc/>
        public static BlockSyntax WithPrependedStatement(this BlockSyntax block, StatementSyntax statement)
            => block.WithPrependedStatement(new[] { statement });

        /// <summary>
        /// Turns block statements that have nested block statements into a
        /// single list of statements. This takes into account labels.
        /// </summary>
        public static BlockSyntax Flattened(this BlockSyntax block) {
            List<SyntaxNode> nestedNodes = new();
            // Add all direct blocks and blocks that are labeled.
            foreach (var s in block.Statements) {
                var statement = s;
                while (statement is LabeledStatementSyntax label)
                    statement = label.Statement;
                if (statement is BlockSyntax)
                    nestedNodes.Add(s);
            }

            // Create a new block by replacing everything with its flattening.
            foreach (var node in nestedNodes) {
                if (node is BlockSyntax b)
                    block = block.ReplaceNode(node, b.Flattened().Statements);
                else if (node is LabeledStatementSyntax l)
                    block = block.ReplaceNode(node, l.Flattened().Statements);
            }
            return block;
        }

        /// <summary>
        /// Turns code of the form
        /// <code>
        ///     label1:
        ///     label2:
        ///         {
        ///         // code
        ///         }
        /// </code>
        /// into
        /// <code>
        ///     {
        ///     label1:
        ///     label2:
        ///         // code
        ///     }
        /// </code>
        /// This requires <paramref name="labeledBlock"/> to label a BlockSyntax.
        /// </summary>
        public static BlockSyntax Flattened(this LabeledStatementSyntax labeledBlock) {
            string labelID = labeledBlock.Identifier.Text;

            // Label the first statement of this flattened block.
            // (Recursion to handle `label: label: label: ...`.)
            BlockSyntax block;
            if (labeledBlock.Statement is LabeledStatementSyntax label) {
                block = label.Flattened();
            } else if (labeledBlock.Statement is BlockSyntax b) {
                block = b;
            } else 
                throw new System.ArgumentException("This may only be called on labeled blocks.", nameof(labeledBlock));

            var first = block.Statements.First();
            return block.ReplaceNode(first, SyntaxFactory.LabeledStatement(labelID, first));
        }
    }
}
