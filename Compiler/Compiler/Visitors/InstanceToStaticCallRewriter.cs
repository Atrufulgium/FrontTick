using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any <tt>instance.Call(..)</tt> into <tt>InstanceType.#STATIC-Call(instance, ..)</tt>.
    /// </para>
    /// </summary>
    public class InstanceToStaticCallRewriter : AbstractFullRewriter<CopyInstanceToStaticCallsRewriter> {

        static readonly string staticPrefix = CopyInstanceToStaticCallsRewriter.staticPrefix;

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
            var callSymbol = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(node).Symbol;
            // Static => nothing to do
            if (callSymbol.IsStatic)
                return base.VisitInvocationExpression(node);

            if (node.Expression is not MemberAccessExpressionSyntax access)
                throw new System.NotImplementedException("Only simple member accessor instance calls are supported");
            var name = access.Name;
            var nameSymbol = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(name).Symbol;
            var typeSymbol = nameSymbol.ContainingType;
            var typeSymbolName = typeSymbol.ToString();

            // aaaaargh
            if (CurrentSemantics.TypesMatch(typeSymbol, MCMirrorTypes.Bool))
                typeSymbolName = MCMirrorTypes.BoolAltName;
            else if (CurrentSemantics.TypesMatch(typeSymbol, MCMirrorTypes.Float))
                typeSymbolName = MCMirrorTypes.FloatAltName;
            else if (CurrentSemantics.TypesMatch(typeSymbol, MCMirrorTypes.Int))
                typeSymbolName = MCMirrorTypes.IntAltName;
            
            return InvocationExpression(
                MemberAccessExpression(
                    typeSymbolName,
                    staticPrefix + name
                ),
                node.ArgumentList.WithPrependedArguments(
                    Argument(
                        access.Expression
                    ).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                )
            );
        }
    }
}
