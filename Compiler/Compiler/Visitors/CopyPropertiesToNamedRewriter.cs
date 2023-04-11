using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
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

        string currentTypeName;

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
            properties.Clear();
            currentTypeName = (CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            return AddCasts(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
            properties.Clear();
            currentTypeName = (CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            return AddCasts(node);
        }

        SyntaxNode AddCasts(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();
            foreach(var prop in properties) {
                var typeSyntax = prop.Type;

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

                    var methodDeclaration =
                        MethodDeclaration(
                            retType, Identifier(methodName)
                        ).WithAttributeLists(accessor.AttributeLists)
                         .WithModifiers(accessor.Modifiers)
                         .WithParameterList(parameters)
                         .WithBody(accessor.Body);
                    newMethods.Add(methodDeclaration);

                    // Don't forget to register with the namemanager!
                    string fullyQualifiedName = $"{currentTypeName}.{methodName}";
                    string name = $"internal/{fullyQualifiedName}";
                    name = NameManager.NormalizeFunctionName(name);
                    nameManager.RegisterMethodname(CurrentSemantics, methodDeclaration, name, this, fullyQualifiedName: fullyQualifiedName);
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
