using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Registers designated methods for use with <see cref="NameManager"/>.
    /// </summary>
    public abstract class AbstractRegisterMethodsByPrefixWalker : AbstractFullWalker {

        /// <summary>
        /// Strings that, if the identifier starts with one, means that this
        /// method should be registered with <see cref="NameManager"/>.
        /// </summary>
        public abstract string[] CharacteristicString { get; }
        public virtual bool IsInternal { get => true; }

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax method) {
            string methodName = method.Identifier.Text;
            foreach (var s in CharacteristicString)
                if (methodName.Contains(s))
                    nameManager.RegisterMethodname(CurrentSemantics, method, this, isInternal: IsInternal);
            // Don't visit children because no need to.
        }
    }
}
