﻿using Microsoft.CodeAnalysis;
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
            => SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(from arg in args select Argument(arg)));

        // I don't like BinaryExpression(<syntaxkind>, ...), so variants here.

        public static BinaryExpressionSyntax BinaryEqualsExpression(ExpressionSyntax left, ExpressionSyntax right)
            => SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, left, right);

        // I hate `ExpressionStatement([..]ExpressionSyntax)`, so I'm adding these shortcuts as needed.
        // I could autogen these but I'm *lazy*.

        public static StatementSyntax AssignmentStatement(
            SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right
        ) => SyntaxFactory.ExpressionStatement(AssignmentExpression(kind, left, right));

        public static StatementSyntax SimpleAssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
            => AssignmentStatement(SyntaxKind.SimpleAssignmentExpression, left, right);

        public static StatementSyntax AddAssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
            => ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    left,
                    right
                )
            );

        public static StatementSyntax SubtractAssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
            => ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SubtractAssignmentExpression,
                    left,
                    right
                )
            );

        // Also, literals are ew

        public static LiteralExpressionSyntax NumericLiteralExpression(int value)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));

        public static LiteralExpressionSyntax NumericLiteralExpression(uint value)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));

        public static LiteralExpressionSyntax StringLiteralExpression(string str)
            => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str));

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

        // Yes this shorthand is clearly necessary.

        public static MemberAccessExpressionSyntax MemberAccessExpression(INamedTypeSymbol type, string name)
            => MemberAccessExpression(type.Name, name);

        public static MemberAccessExpressionSyntax MemberAccessExpression(string type, string name)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(type),
                IdentifierName(name)
            );

        /// <summary>
        /// Parses a string with possible `<c>.</c>`'s into a proper name.
        /// Requires at least one dot.
        /// </summary>
        public static MemberAccessExpressionSyntax MemberAccessExpression(string fullyQualified) {
            var parts = fullyQualified.Split('.');
            if (parts.Length < 2)
                throw new System.ArgumentException("The given MemberAccessExpression string does not represent any accessing.", nameof(fullyQualified));

            ExpressionSyntax lhs = IdentifierName(parts[0]);
            for (int i = 1; i < parts.Length; i++)
                lhs = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lhs, IdentifierName(parts[i]));
            return (MemberAccessExpressionSyntax)lhs;
        }

        /// <summary>
        /// Parses a string with possibly '<tt>.</tt>'s into a proper qualified
        /// name.
        /// </summary>
        public static NameSyntax QualifiedName(string identifiername) {
            var parts = identifiername?.Split('.');
            if (parts?.Length < 2)
                throw new System.ArgumentException("The given MemberAccessExpression string does not represent any accessing.", nameof(identifiername));

            NameSyntax lhs = IdentifierName(parts[0]);
            for (int i = 1; i < parts.Length; i++)
                lhs = SyntaxFactory.QualifiedName(lhs, IdentifierName(parts[i]));

            return lhs;
        }

        public static MemberAccessExpressionSyntax ThisAccessExpression(string name)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(name)
            );

        public static TypeSyntax Type(ITypeSymbol type)
            => ParseTypeName(type.ToDisplayString());

        public static TypeSyntax Type(string type)
            => ParseTypeName(type);

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(ITypeSymbol type, string identifiername)
            => SyntaxFactory.LocalDeclarationStatement(
                VariableDeclaration(type, identifiername)
            );

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(TypeSyntax type, string identifiername)
            => SyntaxFactory.LocalDeclarationStatement(
                VariableDeclaration(type, identifiername)
            );

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(ITypeSymbol type, string identifiername, ExpressionSyntax value)
            => SyntaxFactory.LocalDeclarationStatement(
                VariableDeclaration(type, identifiername, value)
            );

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(TypeSyntax type, string identifiername, ExpressionSyntax value)
            => SyntaxFactory.LocalDeclarationStatement(
                VariableDeclaration(type, identifiername, value)
            );

        public static VariableDeclarationSyntax VariableDeclaration(ITypeSymbol type, string identifiername)
            => SyntaxFactory.VariableDeclaration(
                Type(type),
                SeparatedList(
                    VariableDeclarator(Identifier(identifiername))
                )
            );

        public static VariableDeclarationSyntax VariableDeclaration(TypeSyntax type, string identifiername)
            => SyntaxFactory.VariableDeclaration(
                type,
                SeparatedList(
                    VariableDeclarator(Identifier(identifiername))
                )
            );

        public static VariableDeclarationSyntax VariableDeclaration(ITypeSymbol type, string identifiername, ExpressionSyntax value)
            => SyntaxFactory.VariableDeclaration(
                Type(type),
                SeparatedList(
                    VariableDeclarator(
                        Identifier(identifiername),
                        default,
                        EqualsValueClause(
                            value
                        )
                    )
                )
            );

        public static VariableDeclarationSyntax VariableDeclaration(TypeSyntax type, string identifiername, ExpressionSyntax value)
            => SyntaxFactory.VariableDeclaration(
                type,
                SeparatedList(
                    VariableDeclarator(
                        Identifier(identifiername),
                        default,
                        EqualsValueClause(
                            value
                        )
                    )
                )
            );

        public static ParameterSyntax Parameter(ITypeSymbol type, string identifiername)
            => SyntaxFactory.Parameter(Identifier(identifiername))
                .WithType(Type(type));

        public static ParameterListSyntax ParameterList(params ParameterSyntax[] parameters)
            => SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(parameters)
            );

        public static SeparatedSyntaxList<T> SeparatedList<T>(T item) where T : SyntaxNode
            => SyntaxFactory.SeparatedList(new T[] { item });

        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(params InterpolatedStringContentSyntax[] contents)
            => SyntaxFactory.InterpolatedStringExpression(
                Token(
                    SyntaxKind.InterpolatedStringStartToken
                ),
                List(contents)
            );

        public static ElseClauseSyntax ElseClause(IEnumerable<StatementSyntax> statements)
            => SyntaxFactory.ElseClause(
                Block(statements)
            );

        public static ElseClauseSyntax ElseClause(params StatementSyntax[] statements)
            => SyntaxFactory.ElseClause(
                Block(statements)
            );

        public static ExpressionSyntax InvocationExpression(ExpressionSyntax expression, ExpressionSyntax oneArgument)
            => SyntaxFactory.InvocationExpression(expression, ArgumentList(oneArgument));

        public static StatementSyntax InvocationStatement(ExpressionSyntax expression)
            => SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(expression));

        public static StatementSyntax InvocationStatement(ExpressionSyntax expression, ArgumentListSyntax argumentList)
            => SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(expression, argumentList));

        public static StatementSyntax InvocationStatement(ExpressionSyntax expression, ExpressionSyntax oneArgument)
            => SyntaxFactory.ExpressionStatement(InvocationExpression(expression, oneArgument));
    }
}
