using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all property-related rewriters into one class.
    /// (Also affects all other arrow statements, but that's fine.)
    /// </summary>
    /// <remarks>
    /// This introduces arithmetic of the form `a ∘ b`, so put this before
    /// anything handling that. Yes I need an <tt>IBefore&lt;..&gt;</tt>.
    /// </remarks>
    public class PropertyCategory : AbstractFullWalker<
        ArrowRewriter,
        AutoPropertyRewriter,
        CopyPropertiesToNamedRewriter,
        RegisterPropertiesWalker,
        PropertiesToMethodCallsRewriter,
        RemovePropertiesRewriter
        > { }

    public class RegisterPropertiesWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "GET-", "SET-" };
    }

    /// <summary> Removes any property member. </summary>
    public class RemovePropertiesRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) => null;
    }
}
