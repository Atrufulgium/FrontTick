using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Extension for <see cref="SyntaxFactory"/>.
    /// </summary>
    public static class SyntaxFactoryHelpers {

        public static GotoStatementSyntax GotoStatement(string identifier)
            => SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, IdentifierName(identifier));

        public static SingleVariableDesignationSyntax SingleVariableDesignation(string identifier)
            => SyntaxFactory.SingleVariableDesignation(Identifier(identifier));

        // I don't like BinaryExpression(<syntaxkind>, ...), so variants here.

        public static BinaryExpressionSyntax BinaryEqualsExpression(ExpressionSyntax left, ExpressionSyntax right)
            => BinaryExpression(SyntaxKind.EqualsExpression, left, right);

        // I hate `ExpressionStatement([..]ExpressionSyntax)`, so I'm adding these shortcuts as needed.
        // I could autogen these but I'm *lazy*.

        public static StatementSyntax AssignmentStatement(
            SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right
        ) => ExpressionStatement(AssignmentExpression(kind, left, right));

        public static StatementSyntax SimpleAssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
            => AssignmentStatement(SyntaxKind.SimpleAssignmentExpression, left, right);

        // Also, literals are ew

        public static LiteralExpressionSyntax NumericLiteralExpression(int value)
            => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));
    }
}
