using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any function call to <see cref="MCMirror.Internal.CompileTime.VarName(int)"/>
    /// into a literal string. If contained in an interpolated string, embeds
    /// it literally.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This should later be replaced with a more general framework for
    /// compile-time manipulations.
    /// </remarks>
    public class VarNameMethodRewriter : AbstractFullRewriter {

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
            if (node.Expression is InvocationExpressionSyntax call) {
                MCFunctionName methodName = nameManager.GetMethodName(CurrentSemantics, call, this);
                if (methodName == "VarName") {
                    ExpressionSyntax arg = call.ArgumentList.Arguments[0].Expression;
                    if (arg is IdentifierNameSyntax or MemberAccessExpressionSyntax)
                        return LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(nameManager.GetVariableName(CurrentSemantics, arg, this))
                        );
                    AddCustomDiagnostic(DiagnosticRules.VarNameArgMustBeIdentifier, node.GetLocation());
                    return null;
                }
            }
            return base.VisitInterpolation(node);
        }
    }
}
