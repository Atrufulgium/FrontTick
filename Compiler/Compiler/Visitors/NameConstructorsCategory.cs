using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes all nontrivial <tt>new</tt>s and replaces them with method calls.
    /// </para>
    /// <para>
    /// Also does the other constructor processing we need.
    /// </para>
    /// </summary>
    public class NameConstructorsCategory : AbstractCategory<
        MemberInitToConstructors,
        CopyConstructorsToNamedRewriter,
        RegisterConstructorsWalker,
        ConstructorsToMethodCallsRewriter,
        RemoveConstructorsRewriter
    > { }

    public class RegisterConstructorsWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "-CONSTRUCT-", "-CONSTRUCTSTATIC-" };
    }

    /// <summary> Removes any constructor. </summary>
    public class RemoveConstructorsRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) => null;
        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) => null;
    }
}
