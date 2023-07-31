using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;

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

            if (methodName == "CompileTime/VarName") {
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
            }

            return base.VisitInvocationExpression(node);
        }
    }
}
