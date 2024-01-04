using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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
        /// <see cref="TryGetSemanticAttributeOfType(SemanticModel, MemberDeclarationSyntax, string, out AttributeData)"/>.
        /// </remarks>
        public static bool TryGetAttributeOfType(
            this SemanticModel semantics,
            MemberDeclarationSyntax node,
            string attributeType,
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
        /// <see cref="TryGetAttributeOfType(SemanticModel, MemberDeclarationSyntax, string, out AttributeSyntax)"/>.
        /// </remarks>
        public static bool TryGetSemanticAttributeOfType(
            this SemanticModel semantics,
            MemberDeclarationSyntax node,
            string attributeType,
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

        // Note to self for future: https://stackoverflow.com/a/33966036
        // For when I also want to maybe take into account generics here.

        /// <summary>
        /// Compares a syntax tree node's type to a string type, such as
        /// <c>"MCMirror.Internal.NoCompileAttribute"</c>. The match must be
        /// exact.
        /// </summary>
        /// <remarks>
        /// This does not work for generic types.
        /// </remarks>
        public static bool TypesMatch(this SemanticModel semantics, SyntaxNode node, string typeName) {
            var typeSymbol = semantics.GetTypeInfo(node).Type;
            typeSymbol ??= (ITypeSymbol)semantics.GetDeclaredSymbol(node);
            return semantics.TypesMatch(typeSymbol, typeName);
        }

        /// <summary>
        /// Compares two syntax tree nodes' types to eachother according to the
        /// semantics' interpretation of those nodes' types, and returns
        /// whether the two types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, string)"/>
        public static bool TypesMatch(this SemanticModel semantics, SyntaxNode node, SyntaxNode other) {
            var typeSymbol = semantics.GetTypeInfo(node).Type;
            typeSymbol ??= (ITypeSymbol)semantics.GetDeclaredSymbol(node);
            var otherTypeSymbol = semantics.GetTypeInfo(other).Type;
            otherTypeSymbol ??= (ITypeSymbol)semantics.GetDeclaredSymbol(other);
            return semantics.TypesMatch(typeSymbol, otherTypeSymbol);
        }

        /// <summary>
        /// Compares a type in the syntax tree to a type name according to the
        /// semantics' interpretation of that type, and returns whether the two
        /// types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, string)"/>
        public static bool TypesMatch(this SemanticModel semantics, ITypeSymbol typeSymbol, string typeName) {
            var typeString = typeSymbol.ToDisplayString();
            return typeString == typeName;
        }

        /// <summary>
        /// Compares two types in the syntax tree to eachother according to the
        /// semantics' interpretation of those types, and returns whether the
        /// two types match.
        /// </summary>
        /// <inheritdoc cref="TypesMatch(SemanticModel, SyntaxNode, string)"/>
        public static bool TypesMatch(this SemanticModel semantics, ITypeSymbol typeSymbol, ITypeSymbol otherTypeSymbol) {
            return SymbolEqualityComparer.Default.Equals(typeSymbol, otherTypeSymbol);
        }

        // Taken from https://stackoverflow.com/a/30445814
        /// <summary>
        /// Returns all types higher in the inheritance tree from this type and
        /// itself.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this SemanticModel semantics, SyntaxNode node) {
            var typeSymbol = semantics.GetTypeInfo(node).Type;
            return semantics.GetBaseTypesAndThis(typeSymbol);
        }

        /// <inheritdoc cref="GetBaseTypesAndThis(SemanticModel, SyntaxNode)"/>
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this SemanticModel semantics, ITypeSymbol type) {
            var current = type;
            while (current != null) {
                yield return current;
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Returns all members of this type regardless of accessibility modifier
        /// and including inheritance.
        /// </summary>
        public static IEnumerable<ISymbol> GetAllMembers(this SemanticModel semantics, SyntaxNode node)
            => semantics.GetAllMembers(semantics.GetTypeInfo(node).Type);

        /// <inheritdoc cref="GetAllMembers(SemanticModel, SyntaxNode)"/>
        public static IEnumerable<ISymbol> GetAllMembers(this SemanticModel semantics, ITypeSymbol type)
            => semantics.GetBaseTypesAndThis(type).SelectMany(n => n.GetMembers());

        /// <summary>
        /// Returns all fields of this type regardless of accessibility modifier
        /// and including inheritance.
        /// </summary>
        // TODO: There is a `.IsDefinition` for e.g. inheritance, look into that.
        public static IEnumerable<ISymbol> GetAllFields(this SemanticModel semantics, SyntaxNode node)
            => semantics.GetAllFields(semantics.GetTypeInfo(node).Type);

        /// <inheritdoc cref="GetAllFields(SemanticModel, SyntaxNode)"/>
        public static IEnumerable<ISymbol> GetAllFields(this SemanticModel semantics, ITypeSymbol type)
            => from m in semantics.GetAllMembers(type) where m.Kind == SymbolKind.Field select m;

        /// <summary>
        /// Returns all methods of this type regardless of accessibility modifier
        /// and including inheritance.
        /// </summary>
        public static IEnumerable<ISymbol> GetAllMethods(this SemanticModel semantics, SyntaxNode node)
            => semantics.GetAllMethods(semantics.GetTypeInfo(node).Type);

        /// <inheritdoc cref="GetAllMethods(SemanticModel, SyntaxNode)"/>
        public static IEnumerable<ISymbol> GetAllMethods(this SemanticModel semantics, ITypeSymbol type)
            => from m in semantics.GetAllMembers(type) where m.Kind == SymbolKind.Method select m;

        // See https://stackoverflow.com/a/43949455
        public static bool IsPrimitive(this ITypeSymbol type) {
            switch (type.SpecialType) {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the fully qualified name of a symbol.
        /// </summary>
        /// <remarks>
        /// This does not go further than regular namespaces. No global:: or
        /// assembly names or similar are prefixed ever.
        /// </remarks>
        public static string FullyQualifiedName(this ISymbol symbol) {
            var container = symbol.ContainingSymbol;
            if (container == null || (container is INamespaceSymbol namespaceSymbol && namespaceSymbol.IsGlobalNamespace))
                return symbol.Name;
            return $"{FullyQualifiedName(container)}.{symbol.Name}";
        }

        /// <summary>
        /// <para>
        /// Gets the fully qualified name of a type symbol.
        /// </para>
        /// <para>
        /// For primitives, this returns the fully qualified name (e.g. `float`
        /// gives `System.Single`). This is because Roslyn cannot see any
        /// <c>primitive.member</c> but can see <c>System.Primitive.member</c>
        /// for whatever reason.
        /// </para>
        /// </summary>
        public static string GetFullyQualifiedNameIncludingPrimitives(this SemanticModel semantics, ITypeSymbol typeSymbol) {
            var typeSymbolName = typeSymbol.ToString();

            // blergh
            if (semantics.TypesMatch(typeSymbol, MCMirrorTypes.Bool))
                typeSymbolName = MCMirrorTypes.BoolFullyQualified;
            else if (semantics.TypesMatch(typeSymbol, MCMirrorTypes.Float))
                typeSymbolName = MCMirrorTypes.FloatFullyQualified;
            else if (semantics.TypesMatch(typeSymbol, MCMirrorTypes.Int))
                typeSymbolName = MCMirrorTypes.IntFullyQualified;
            else if (semantics.TypesMatch(typeSymbol, MCMirrorTypes.UInt))
                typeSymbolName = MCMirrorTypes.UIntFullyQualified;
            return typeSymbolName;
        }
    }
}
