using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This rewriter turns code of the form
    /// <code>
    ///     MyProperty {get; set} = value;
    /// </code>
    /// into code of the form
    /// <code>
    ///     backingField = value;
    ///     MyProperty { get { return backingField; } set { backingField = value; } }
    /// </code>
    /// </summary>
    public class AutoPropertyRewriter : AbstractFullRewriter {

        readonly List<MemberDeclarationSyntax> introducedFields = new();

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitClassDeclarationRespectingNoCompile);
        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitStructDeclarationRespectingNoCompile);

        SyntaxNode VisitTypeDeclarationSyntax<T>(T node, Func<T, SyntaxNode> baseCall)
            where T : TypeDeclarationSyntax {
            introducedFields.Clear();
            node = (T) baseCall(node);
            return node.WithAdditionalMembers<T>(introducedFields);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            // Only non-null if it's an autoproperty with value.
            var autoValue = node.Initializer;
            // Auto property only if all (iff any) accessor is simply get; or set;.
            // (Or init;, but not supporting that (yet).)
            var firstAccessor = node.AccessorList.Accessors.First();
            bool isAutoProperty = firstAccessor.Body == null && firstAccessor.ExpressionBody == null;
            if (!isAutoProperty)
                return node;

            // We are now an auto property. Auto properties are either get or
            // getset, so it's still a bit obnoxious.
            string fieldName = $"#AUTOPROPERTY#{node.Identifier.Text}";
            var type = CurrentSemantics.GetTypeInfo(node.Type).Type;

            FieldDeclarationSyntax declaration;
            if (autoValue != null)
                declaration = FieldDeclaration(VariableDeclaration(type, fieldName, autoValue.Value));
            else
                declaration = FieldDeclaration(VariableDeclaration(type, fieldName));
            declaration = declaration.WithModifiers(node.Modifiers);
            introducedFields.Add(declaration);

            List <AccessorDeclarationSyntax> newAccessors = new(2);
            foreach (var accessor in node.AccessorList.Accessors) {
                if (accessor.Keyword.IsKind(SyntaxKind.GetKeyword))
                    newAccessors.Add(accessor.WithBody(
                        Block(
                            ReturnStatement(
                                IdentifierName(fieldName)
                            )
                        )
                    ));
                else
                    newAccessors.Add(accessor.WithBody(
                        Block(
                            SimpleAssignmentStatement(
                                IdentifierName(fieldName),
                                IdentifierName("value")
                            )
                        )
                    ));
            }

            if (autoValue != null)
                node = node.WithInitializer(null);
            var newAccessorList = node.AccessorList.WithAccessors(new(newAccessors));
            return node.WithAccessorList(newAccessorList);
        }
    }
}
