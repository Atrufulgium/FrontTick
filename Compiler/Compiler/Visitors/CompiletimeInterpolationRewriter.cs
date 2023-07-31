using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns all string interpolations into full strings. Non-compiletime
    /// string interpolations result in an error.
    /// </para>
    /// </summary>
    public class CompiletimeInterpolationRewriter : AbstractFullRewriter<CompiletimeClassRewriter> {

        // Note:
        // InterpolatedStringExpression is the full `$" .. "`
        // InterpolatedStringText is any literal text that is not an interpolation.
        // Interpolation is any `{interpolation}` in the string.
        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node) {
            var newContent = new List<InterpolatedStringContentSyntax>();
            foreach (var part in node.Contents) {
                if (part is InterpolatedStringTextSyntax t) {
                    newContent.Add(
                        (InterpolatedStringTextSyntax)VisitInterpolatedStringText(t)
                    );
                } else if (part is InterpolationSyntax i) {
                    var newPart = VisitInterpolation(i);
                    if (newPart is LiteralExpressionSyntax lit)
                        newContent.Add(
                            InterpolatedStringText(lit.Token.ValueText)
                        );
                    else
                        newContent.Add((InterpolationSyntax)newPart);
                }
            }
            // Now we may have introduced multiple Texts in a row. Collapse 'em.
            var newnewContent = new List<InterpolatedStringContentSyntax>();
            string textSoFar = "";
            foreach (var part in newContent) {
                if (part is InterpolatedStringTextSyntax t) {
                    textSoFar += t.TextToken.Text;
                } else if (part is InterpolationSyntax) {
                    if (textSoFar != "") {
                        newnewContent.Add(
                            InterpolatedStringText(textSoFar)
                        );
                        textSoFar = "";
                    }
                    newnewContent.Add(part);
                }
            }
            if (textSoFar != "") {
                newnewContent.Add(
                    InterpolatedStringText(textSoFar)
                );
            }
            // Now we have 1+ entries in our string. If we have one entry, and
            // it's text, return a regular string. Otherwise, interpolated.
            if (newnewContent.Count == 1
                && newnewContent[0] is InterpolatedStringTextSyntax text) {
                return LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(text.TextToken.Text)
                );
            }
            return InterpolatedStringExpression(node.StringStartToken, new(newnewContent), node.StringEndToken);
        }

        public override SyntaxNode VisitInterpolation(InterpolationSyntax node) {
            // The good case
            if (node.Expression is LiteralExpressionSyntax lit) {
                if (lit.Kind() == SyntaxKind.StringLiteralExpression)
                    return lit;
                else
                    return StringLiteralExpression(lit.Token.Value.ToString());
            }

            // The medium case: we may be constant, but don't know the
            // resulting value because it depends on semantics.
            var symbol = CurrentSemantics.GetSymbolInfo(node.Expression).Symbol;
            if (symbol is IFieldSymbol field && field.IsConst)
                return StringLiteralExpression(field.ConstantValue.ToString());

            // The bad case: not constant and we should throw an error.
            AddCustomDiagnostic(DiagnosticRules.StringInterpolationsMustBeConstant, node.GetLocation());
            return StringLiteralExpression("(non-constant string)");
        }
    }
}
