using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {

    // This depends on RegisterMethodsWalker to ensure the renamed methods, if
    // any, do not get their name registered yet.
    // The reason for this is that any method rewrite scheme uses
    /// <see cref="AbstractRegisterMethodsByPrefixWalker"/>
    // with a characteristic string, *which this will obviously match* as we
    // are explicitely trying to imitate it.
    // This assumption remains valid as long as all method-introducing phases
    // get their methods registered by the prefix walker.

    /// <summary>
    /// <para>
    /// Replaces ー with - and ⵌ with # in variables and method names so comparison
    /// of generated code to regular code can be done.
    /// </para>
    /// <para>
    /// This can freely be omitted from regular, non-CompilerTests-project
    /// compilations.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Do not add this manually to your compilation phases in testing.
    /// That is the job of TestHelpers.cs.
    /// </remarks>
    public class MakeCompilerTestingEasierRewriter : AbstractFullRewriter<RegisterMethodsWalker> {

        const char fakeMinus = 'ー';
        const char fakePound = 'ⵌ';

        public override SyntaxToken VisitToken(SyntaxToken token) {
            if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken)) {
                string id = (string)token.Value;
                id = id.Replace(fakeMinus, '-').Replace(fakePound, '#');
                return Identifier(id);
            }
            return base.VisitToken(token);
        }
    }
}