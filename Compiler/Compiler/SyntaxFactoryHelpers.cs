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

    }
}
