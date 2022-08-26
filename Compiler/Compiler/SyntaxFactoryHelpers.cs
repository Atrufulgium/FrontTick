using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Extension for <see cref="SyntaxFactory"/>.
    /// </summary>
    public static class SyntaxFactoryHelpers {

        public static GotoStatementSyntax GotoStatement(string identifier)
            => SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(identifier));


    }
}
