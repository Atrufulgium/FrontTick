using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Extension methods needing a semantic model. All of these extension
    /// methods are attached to <see cref="SemanticModel"/>.
    /// </summary>
    public static class SemanticExtensionMethods {

        /// <summary>
        /// If this declaration has an attribute of a certain type, this method
        /// returns true, and puts the first it finds in <c>outAttribute</c>.
        /// Otherwise, this returns false.
        /// </summary>
        /// <param name="semantics">
        /// The semantics context.
        /// </param>
        /// <param name="node">
        /// The node to search for an attached attribute.
        /// </param>
        /// <param name="attributeType">
        /// The type of the attribute to search for.
        /// </param>
        /// <param name="outAttribute">
        /// Where the attribute gets put if it gets found.
        /// </param>
        /// <remarks>
        /// This is the syntactic version of
        /// <see cref="TryGetSemanticAttributeOfType(SemanticModel, MemberDeclarationSyntax, Type, out AttributeData)"/>.
        /// </remarks>
        public static bool TryGetAttributeOfType(
            this SemanticModel semantics,
            MemberDeclarationSyntax node,
            Type attributeType,
            out AttributeSyntax outAttribute
        ) {
            // MemberDeclarationSyntax is super to both all ways to declare
            // methods, classes/interfaces/structs/etc, and probably more I
            // forget, but probably comprehensive enough to encompass all.
            SyntaxList<AttributeListSyntax> attributeLists = node.AttributeLists;

            // An AttributeList is a full
            // `[LoremIpsum("Dolor Sit"), TheAnswer(42)]`
            // I didn't even know c# supported this syntax.
            foreach (var attributeList in attributeLists) {
                foreach (var attribute in attributeList.Attributes) {
                    if (semantics.TypesMatch(attribute, attributeType)) {
                        outAttribute = attribute;
                        return true;
                    }
                }
            }

            outAttribute = null;
            return false;
        }

        /// <summary>
        /// If this declaration has an attribute of a certain type, this method
        /// returns true, and puts the first it finds in <c>outAttribute</c>.
        /// Otherwise, this returns false.
        /// </summary>
        /// <param name="semantics">
        /// The semantics context.
        /// </param>
        /// <param name="node">
        /// The node to search for an attached attribute.
        /// </param>
        /// <param name="attributeType">
        /// The type of the attribute to search for.
        /// </param>
        /// <param name="outAttribute">
        /// Where the attribute gets put if it gets found.
        /// </param>
        /// <remarks>
        /// This is the semantic version of
        /// <see cref="TryGetAttributeOfType(SemanticModel, MemberDeclarationSyntax, Type, out AttributeSyntax)"/>.
        /// </remarks>
        public static bool TryGetSemanticAttributeOfType(
            this SemanticModel semantics,
            MemberDeclarationSyntax node,
            Type attributeType,
            out AttributeData outAttribute
        ) {
            var nodeModel = semantics.GetDeclaredSymbol(node);
            var attributes = nodeModel.GetAttributes();

            foreach(var attribute in attributes) {
                if (semantics.TypesMatch(attribute.AttributeClass, attributeType)) {
                    outAttribute = attribute;
                    return true;
                }
            }

            outAttribute = null;
            return false;
        }

        /// <summary>
        /// Return the full name (namespace.class.method) of a method.
        /// </summary>
        public static string GetFullyQualifiedMethodName(this SemanticModel semantics, MethodDeclarationSyntax method) {
            var methodModel = semantics.GetDeclaredSymbol(method);
            var containingType = methodModel.ContainingType;
            string methodName = methodModel.Name;
            string containingName = containingType.ToString();
            return $"{containingName}.{methodName}";
        }

        // Note to self for future: https://stackoverflow.com/a/33966036
        // For when I also want to maybe take into account generics here.

        /// <summary>
        /// Compares a syntax tree node's type to a known type according to the
        /// semantics' interpretation of that node's type, and returns whether
        /// the two types match.
        /// </summary>
        /// <remarks>
        /// This does not work for generic types.
        /// </remarks>
        public static bool TypesMatch(this SemanticModel semantics, SyntaxNode node, Type other) {
            var typeSymbol = semantics.GetTypeInfo(node).Type;
            return semantics.TypesMatch(typeSymbol, other);
        }

        /// <summary>
        /// Compares two syntax tree nodes' types to eachother according to the
        /// semantics' interpretation of those nodes' types, and returns
        /// whether the two types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, Type)"/>
        public static bool TypesMatch(this SemanticModel semantics, SyntaxNode node, SyntaxNode other) {
            var typeSymbol = semantics.GetTypeInfo(node).Type;
            var otherTypeSymbol = semantics.GetTypeInfo(other).Type;
            return semantics.TypesMatch(typeSymbol, otherTypeSymbol);
        }

        /// <summary>
        /// Compares a type in the syntax tree to a known type according to the
        /// semantics' interpretation of that type, and returns whether the two
        /// types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, Type)"/>
        public static bool TypesMatch(this SemanticModel semantics, ITypeSymbol typeSymbol, Type other) {
            var otherTypeSymbol = semantics.Compilation.GetTypeByMetadataName(other.FullName);
            return semantics.TypesMatch(typeSymbol, otherTypeSymbol);
        }

        /// <summary>
        /// Compares two types in the syntax tree to eachother according to the
        /// semantics' interpretation of those types, and returns whether the
        /// two types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, Type)"/>
        public static bool TypesMatch(this SemanticModel semantics, ITypeSymbol typeSymbol, ITypeSymbol otherTypeSymbol) {
            return SymbolEqualityComparer.Default.Equals(typeSymbol, otherTypeSymbol);
        }
    }
}
