using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This category turns instance methods into static methods.
    /// </para>
    /// </summary>
    public class StaticifyInstanceCategory : AbstractFullRewriter<
        ThisRewriter,
        CopyInstanceToStaticCallsRewriter,
        RegisterStaticfiedWalker,
        InstanceToStaticCallRewriter,
        RemoveInstanceCallsRewriter
    > { }

    public class RegisterStaticfiedWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "STATIC-" };
    }

    /// <summary>
    /// <para>
    /// Removes any code of the form
    /// <code>
    ///     public T1 Method(T2 a, .. ) { .. }
    /// </code>
    /// </para>
    /// </summary>
    public class RemoveInstanceCallsRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            if (!node.ChildTokensContain(SyntaxKind.StaticKeyword))
                return null;
            return base.VisitMethodDeclarationRespectingNoCompile(node);
        }
    }
}
