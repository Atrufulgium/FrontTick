using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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
        string consistentTimestamp = null;

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
            // In exceptional cases the name may not exist.
            // (E.g. the rewrites of
            /// <see cref="MakeCompilerTestingEasierRewriter"/>
            //  break the "everything is registered" invariant.)
            // However, in those cases it will definitely not be a built-in
            // like this, so it's fine to ignore those cases.
            if (!nameManager.MethodNameIsRegistered(CurrentSemantics, node))
                return base.VisitInvocationExpression(node);

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
                return HandlePrint(node, arg);
            } else if (methodName == "CompileTime/PrintComplex") {
                return HandlePrintComplex(node, arg);
            } else if (methodName == "CompileTime/Timestamp") {
                consistentTimestamp ??= DateTime.Now.ToString();
                return StringLiteralExpression(consistentTimestamp);
            }

            return base.VisitInvocationExpression(node);
        }

        // Print a basic int.
        SyntaxNode HandlePrint(InvocationExpressionSyntax node, ExpressionSyntax arg) {
            if (arg is not (IdentifierNameSyntax or MemberAccessExpressionSyntax)) {
                AddCustomDiagnostic(DiagnosticRules.PrintArgMustBeIdentifier, node.GetLocation());
                return null;
            }

            string varnameMCFunction = nameManager.GetVariableName(CurrentSemantics, arg, this);
            string varnameCSharp = arg.ToString();

            // Compiler generated varnames are coloured gray+italicised in chat.
            string initialColor = varnameCSharp.StartsWith('#') ? "§7§o" : "§f";

            return node.WithExpression(MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run))
                .WithArgumentList(
                ArgumentList(
                    StringLiteralExpression(
                        "tellraw @a [\"§7[Value of " + initialColor
                        + varnameCSharp
                        + "§r§7: §r\",{\"score\":{\"name\":\""
                        + varnameMCFunction
                        + "\",\"objective\":\"_\"},\"color\":\"yellow\"},\"§7]\"]"
                    )
                )
            );
        }

        // Turns `Print(object)` into a `RunRaw(..)`s in tree-form that prints
        // everything, nested however deeply.
        // Includes compiler-generated values.
        SyntaxNode HandlePrintComplex(InvocationExpressionSyntax node, ExpressionSyntax arg) {
            if (arg is not (IdentifierNameSyntax or MemberAccessExpressionSyntax)) {
                AddCustomDiagnostic(DiagnosticRules.PrintArgMustBeIdentifier, node.GetLocation());
                return null;
            }

            string varnameMCFunction = nameManager.GetVariableName(CurrentSemantics, arg, this);
            string varnameCSharp = arg.ToString();
            var type = CurrentSemantics.GetTypeInfo(arg).Type;

            // Compiler generated varnames are coloured gray+italicised in chat.
            string initialColor = varnameCSharp.StartsWith('#') ? "§7§o" : "§f";

            List<string> tellrawParts = new() {
                $"\"§7[Value of {initialColor}{varnameCSharp}§r§7 (complex type §r{type.Name}§7)]\""
            };

            GetPrintComplexRunrawArgs(type, tellrawParts, varnameMCFunction, 1);

            return node.WithExpression(MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run))
                .WithArgumentList(
                ArgumentList(
                    StringLiteralExpression(
                        $"tellraw @a [{string.Join(',', tellrawParts)}]"
                    )
                )
            );
        }

        // This code somewhat copied from
        /// <see cref="ProcessedToDatapackWalker.AddAssignment(string, string, string, ITypeSymbol)"/>
        void GetPrintComplexRunrawArgs(ITypeSymbol symbol, List<string> tellrawParts, string nameSoFar, int depth) {
            string indent = new(' ', depth);

            // Fields are fine because by this point all tomethod rewriters are done.
            // Compiler generated varnames are coloured gray+italicised in chat.
            foreach (var m in symbol.GetNonstaticFields()) {
                var fieldType = m.Type;
                var name = m.Name;
                var typeName = fieldType.Name;
                string initialColor = name.StartsWith('#') ? "§7§o" : "§f";
                var varnameMCFunction = $"{nameSoFar}#{name}";
                tellrawParts.Add($"\"\\n{indent}{initialColor}{name} §r§7(§f{typeName}§7):\"");
                if (CurrentSemantics.TypesMatch(fieldType, MCMirrorTypes.Int)) {
                    tellrawParts.Add($"\"\\n  {indent}\",{{\"score\":{{\"name\":\"{varnameMCFunction}\",\"objective\":\"_\"}},\"color\":\"yellow\"}}");
                } else {
                    GetPrintComplexRunrawArgs(fieldType, tellrawParts, varnameMCFunction, depth + 2);
                }
            }
        }
    }
}
