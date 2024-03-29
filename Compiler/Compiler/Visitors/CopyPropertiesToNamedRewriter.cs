﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any code of the form
    /// <code>
    ///     public T MyProperty { get .. set .. }
    /// </code>
    /// into code of the form
    /// <code>
    ///     public T MyProperty { get { .. } set { .. } }
    ///     public void GET-MyProperty() { .. }
    ///     public void SET-MyProperty() { .. }
    /// </code>
    /// </para>
    /// <para>
    /// In particular, this assumes that all properties have been written out
    /// fully with no sugar of arrow notation. Also no <tt>init</tt> support.
    /// </para>
    /// </summary>
    public class CopyPropertiesToNamedRewriter : AbstractFullRewriter {

        readonly List<PropertyDeclarationSyntax> properties = new();

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node) {
            properties.Clear();
            node = (StructDeclarationSyntax)base.VisitStructDeclarationRespectingNoCompile(node);
            return AddProperties(node);
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            properties.Clear();
            node = (ClassDeclarationSyntax)base.VisitClassDeclarationRespectingNoCompile(node);
            return AddProperties(node);
        }

        SyntaxNode AddProperties(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();
            foreach(var prop in properties) {
                var typeSyntax = prop.Type;
                var globalModifiers = prop.Modifiers;

                foreach (var accessor in prop.AccessorList.Accessors) {
                    TypeSyntax retType;
                    ParameterListSyntax parameters;
                    string methodName;

                    if (accessor.Keyword.IsKind(SyntaxKind.SetKeyword)) {
                        retType = PredefinedType(Token(SyntaxKind.VoidKeyword));
                        parameters = ParameterList(
                            Parameter(
                                attributeLists: default,
                                modifiers: default,
                                type: typeSyntax,
                                identifier: Identifier("value"),
                                @default: default
                            )
                        );
                        methodName = GetSetMethodName(prop.Identifier.Text);
                    } else if (accessor.Keyword.IsKind(SyntaxKind.GetKeyword)) {
                        retType = typeSyntax;
                        parameters = ParameterList();
                        methodName = GetGetMethodName(prop.Identifier.Text);
                    } else {
                        AddCustomDiagnostic(DiagnosticRules.Unsupported, accessor.GetLocation(), "init accessor", "Low priority.");
                        return null;
                    }

                    // We do not want to introduce duplicate accessors. So
                    // just replace all accessors with public, as access
                    // semantics have already passed c# compilation.
                    var combinedModifiers = accessor.Modifiers.Union(globalModifiers).Select(t => t.Kind()).ToHashSet();
                    foreach (var token in new SyntaxKind[] { SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword })
                        combinedModifiers.Remove(token);
                    combinedModifiers.Add(SyntaxKind.PublicKeyword);

                    var methodDeclaration =
                        MethodDeclaration(
                            retType, Identifier(methodName)
                        ).WithAttributeLists(accessor.AttributeLists)
                         .WithModifiers(new(combinedModifiers.Select(k => Token(k))))
                         .WithParameterList(parameters)
                         .WithBody(accessor.Body);
                    newMethods.Add(methodDeclaration);
                }
            }
            node = node.AddMembers(newMethods.ToArray());
            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            properties.Add(node);
            return base.VisitPropertyDeclaration(node);
        }

        public static string GetGetMethodName(string identifier) => $"GET-{identifier}";
        public static string GetSetMethodName(string identifier) => $"SET-{identifier}";
    }
}
