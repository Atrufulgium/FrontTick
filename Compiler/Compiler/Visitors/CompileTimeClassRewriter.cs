using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any function call to <see cref="MCMirror.Internal.CompileTime"/>
    /// methods into their resulting value.
    /// </para>
    /// </summary>
    public class CompiletimeClassRewriter : AbstractFullRewriter {

        string trueLoadValue = null;

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
            MCFunctionName methodName = nameManager.GetMethodName(CurrentSemantics, node, this);
            ExpressionSyntax arg = null;
            if (node.ArgumentList?.Arguments.Count > 0)
                arg = node.ArgumentList.Arguments[0].Expression;

            if (methodName.ToString().Contains("CompileTime/VarName")) {
                if (arg is IdentifierNameSyntax or MemberAccessExpressionSyntax)
                    return StringLiteralExpression(nameManager.GetVariableName(CurrentSemantics, arg, this));
                AddCustomDiagnostic(DiagnosticRules.VarNameArgMustBeIdentifier, node.GetLocation());
                return null;
            } else if (methodName == "CompileTime/MethodName") {
                if (arg is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                    var symbol = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(arg).Symbol;
                    return StringLiteralExpression(nameManager.GetMethodName(CurrentSemantics, symbol, this));
                }
                AddCustomDiagnostic(DiagnosticRules.MethodNameArgMustBeIdentifier, node.GetLocation());
                return null;
            } else if (methodName == "CompileTime/CurrentNamespace") {
                return StringLiteralExpression(nameManager.manespace);
            } else if (methodName == "CompileTime/TrueLoadValue") {
                // Just use a seconds value.
                trueLoadValue ??= ((int)(DateTime.UtcNow.Ticks / 10_000_000)).ToString();
                return StringLiteralExpression(trueLoadValue);
            } else if (methodName == "CompileTime/Print") {
                if (arg is not (IdentifierNameSyntax or MemberAccessExpressionSyntax)) {
                    AddCustomDiagnostic(DiagnosticRules.VarNameArgMustBeIdentifier, node.GetLocation());
                    return null;
                }

                string varnameMCFunction = nameManager.GetVariableName(CurrentSemantics, arg, this);
                string varnameCSharp = node.ArgumentList.Arguments[0].ToString();
                node = node.WithExpression(MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run));
                return node.WithArgumentList(
                    ArgumentList(
                        StringLiteralExpression(
                            "tellraw @a [\"§7[Value of §r"
                            + varnameCSharp
                            + "§7: §r\",{\"score\":{\"name\":\""
                            + varnameMCFunction
                            + "\",\"objective\":\"_\"}},\"§7]\"]"
                        )
                    )
                );
            }

            return base.VisitInvocationExpression(node);
        }
    }
}
