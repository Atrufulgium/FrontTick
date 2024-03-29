﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any code inside class <tt>T</tt> of the form
    /// <code>
    ///     public T1 Method(T2 a, .. ) { .. }
    /// </code>
    /// into code of the form
    /// <code>
    ///     public T1 Method(T2 a, .. ) { .. }
    ///     public static T1 STATIC-Method(T #instance, T2 a) { .. }
    /// </code>
    /// with the same method body (other than turning EXPLICIT <tt>this.</tt>
    /// into <tt>#instance.</tt>).
    /// </para>
    /// </summary>
    public class CopyInstanceToStaticCallsRewriter : AbstractFullRewriter {

        readonly List<MethodDeclarationSyntax> instanceMethods = new();

        ITypeSymbol currentType;

        private static readonly string instancePrefix = "#instance";
        internal static readonly string staticPrefix = "STATIC-";

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node) {
            instanceMethods.Clear();
            currentType = CurrentSemantics.GetDeclaredSymbol(node);
            node = (StructDeclarationSyntax)base.VisitStructDeclarationRespectingNoCompile(node);
            return HandleType(node);
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            instanceMethods.Clear();
            currentType = CurrentSemantics.GetDeclaredSymbol(node);
            node = (ClassDeclarationSyntax)base.VisitClassDeclarationRespectingNoCompile(node);
            return HandleType(node);
        }

        SyntaxNode HandleType(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();

            foreach(var method in instanceMethods) {
                var inList = method.ParameterList
                    .WithPrependedArguments(
                        Parameter(currentType, instancePrefix)
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                    );
                string methodName = $"{staticPrefix}{method.Identifier}";
                var bodyRewriter = new ThisRewriter();
                var methodDeclaration =
                    MethodDeclaration(
                        method.ReturnType, Identifier(methodName)
                    ).WithAttributeLists(method.AttributeLists)
                     .WithModifiers(method.Modifiers.Add(Token(SyntaxKind.StaticKeyword)))
                     .WithBody((BlockSyntax) bodyRewriter.Visit(method.Body)) // note that this updates the thises
                     .WithParameterList(inList);
                newMethods.Add(methodDeclaration);
            }
            node = node.AddMembers(newMethods.ToArray());
            return node;
        }

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            if (!node.ChildTokensContain(SyntaxKind.StaticKeyword))
                instanceMethods.Add(node);
            return base.VisitMethodDeclarationRespectingNoCompile(node);
        }

        /// <summary>
        /// Turns `this` expressions into `#instance`.
        /// </summary>
        private class ThisRewriter : CSharpSyntaxRewriter {
            public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
                => IdentifierName(instancePrefix);
        }
    }
}
