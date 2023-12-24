using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns literals of supported types into constructors with just ints.
    /// </para>
    /// <para>
    /// (Exception: <c>bool</c>'s <c>true</c>/<c>false</c> remain in place.)
    /// </para>
    /// </summary>
    public class RewritePrimitiveLiteralsRewriter : AbstractFullRewriter {

        public unsafe override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node) {
            if (CurrentSemantics.TypesMatch(node, MCMirrorTypes.Float)) {
                float val = float.Parse(node.Token.ValueText);
                if (val == 0 || float.IsSubnormal(val)) {
                    return MemberAccessExpression(MCMirrorTypes.Float_PositiveZero);
                }
                int asInt = *(int*)(&val);
                // IEEE float format
                int mantissa23 = asInt & 0b111_11111_11111_11111_11111;
                int expBias8 = (asInt >> 23) & 0b111_11111;
                bool positive = asInt >= 0;
                // fronttick float format
                int mantissa31 = mantissa23 << 8;
                int exp32 = (expBias8 - 127);
                if (!positive) {
                    mantissa31 = -(mantissa31 + 1);
                }
                return ObjectCreationExpression(
                    PredefinedType(Token(SyntaxKind.FloatKeyword)),
                    ArgumentList(
                        NumericLiteralExpression(mantissa31),
                        NumericLiteralExpression(exp32)
                    ),
                    default
                );
            }
            return base.VisitLiteralExpression(node);
        }

    }
}
