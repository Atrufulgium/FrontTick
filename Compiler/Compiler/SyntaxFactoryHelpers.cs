using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// Extension for <see cref="SyntaxFactory"/>.
    /// </para>
    /// <para>
    /// These are often bare-bones and badly formatted. Since I don't care
    /// about formatting, that is fine.
    /// </para>
    /// </summary>
    public static class SyntaxFactoryHelpers {

        public static GotoStatementSyntax GotoStatement(string identifier)
            => SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, IdentifierName(identifier));

        public static SingleVariableDesignationSyntax SingleVariableDesignation(string identifier)
            => SyntaxFactory.SingleVariableDesignation(Identifier(identifier));

        public static ArgumentListSyntax ArgumentList(params ExpressionSyntax[] args)
            => SyntaxFactory.ArgumentList(SeparatedList(from arg in args select Argument(arg)));

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

        public static InterpolatedStringTextSyntax InterpolatedStringText(string value)
            => SyntaxFactory.InterpolatedStringText().WithTextToken(
                Token(
                    TriviaList(),
                    SyntaxKind.InterpolatedStringTextToken,
                    value,
                    value,
                    TriviaList()
                )
            );
    }
}
