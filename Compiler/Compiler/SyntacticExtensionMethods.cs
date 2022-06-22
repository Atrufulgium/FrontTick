using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    }
}
