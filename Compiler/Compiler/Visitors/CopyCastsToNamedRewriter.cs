using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any code of the form
    /// <code>
    ///     public static implicit operator T1(T2 a) { .. }
    /// </code>
    /// into code of the form
    /// <code>
    ///     public static implicit operator T1(T2 a) { .. }
    ///     public static T1 #CAST#IMPLICIT#T1#T2(T2 a) { .. }
    /// </code>
    /// with the same method body, and the same for explicit casts.
    /// </para>
    /// </summary>
    /// </remarks>
    public class CopyCastsToNamedRewriter : AbstractFullRewriter {

        readonly List<ConversionOperatorDeclarationSyntax> ops = new();

        readonly Dictionary<
            (string retType, string inType),
            (string type, string name)
        > castMethodNames = new();

        string currentTypeName;

        /// <summary>
        /// Given a cast between two types, gives the fully qualified generated
        /// method name. Because of CS0457 and CS0557 this operation is
        /// well-defined.
        /// </summary>
        public (string type, string name) GetMethodName(ITypeSymbol inType, ITypeSymbol outType) {
            if (castMethodNames.TryGetValue((outType.Name, inType.Name), out var name)) {
                return name;
            }
            throw new System.ArgumentException($"There is no cast from {inType.Name} to {outType.Name}. Are these built-in?");
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
            ops.Clear();
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            return AddCasts(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
            ops.Clear();
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            return AddCasts(node);
        }

        SyntaxNode AddCasts(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();
            foreach(var op in ops) {
                var returnType = CurrentSemantics.GetTypeInfo(op.Type).Type.Name;
                var inType = CurrentSemantics.GetTypeInfo(op.ParameterList.Parameters[0].Type).Type.Name;
                var plicity = op.IsImplicitConversion() ? "IMPLICIT" : "EXPLICIT";
                string methodName = $"CAST-{plicity}-{returnType}-{inType}";
                var methodDeclaration =
                    MethodDeclaration(
                        op.Type, Identifier(methodName)
                    ).WithAttributeLists(op.AttributeLists)
                     .WithModifiers(op.Modifiers)
                     .WithBody(op.Body)
                     .WithParameterList(op.ParameterList);
                newMethods.Add(methodDeclaration);

                // Don't forget to register with the namemanager!
                string fullyQualifiedName = $"{currentTypeName}.{methodName}";
                string name = $"internal/{fullyQualifiedName}";
                name = NameManager.NormalizeFunctionName(name);
                nameManager.RegisterMethodname(CurrentSemantics, methodDeclaration, name, this, fullyQualifiedName: fullyQualifiedName);

                // We need to access these method names later.
                // This is a fully qualified name but we need the type and method
                // separately. So split them.
                int i = fullyQualifiedName.LastIndexOf('.');
                castMethodNames.Add(
                    (returnType, inType),
                    (type: fullyQualifiedName[..i], name: fullyQualifiedName[(i + 1)..])
                );
            }
            node = node.AddMembers(newMethods.ToArray());
            return node;
        }

        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) {
            ops.Add(node);
            return base.VisitConversionOperatorDeclaration(node);
        }
    }
}
