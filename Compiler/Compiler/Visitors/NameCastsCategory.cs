using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This category turns both explicit and implicit casts into methods that
    /// are called explicitely.
    /// </para>
    /// </summary>
    public class NameCastsCategory : AbstractFullRewriter<
        CopyCastsToNamedRewriter,
        RegisterCastsWalker,
        CastsToMethodCallsRewriter,
        RemoveCastRewriter
    > { }

    public class RegisterCastsWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "CAST-" };
    }

    /// <summary> Removes any method that is an implicit/explicit cast. </summary>
    public class RemoveCastRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) => null;
    }
}
