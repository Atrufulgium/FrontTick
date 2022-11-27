using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Turns any code of the form
    /// <code>
    ///     public static T1 operator +(T2 a, T3 b) { .. }
    /// </code>
    /// into code of the form
    /// <code>
    ///     public static T1 operator +(T2 a, T3 b) { .. }
    ///     public static T1 operator #ADD(T2 a, T3 b) { .. }
    /// </code>
    /// with the same method body.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This does not support overloading any of
    /// <code> +(T)  -(T)  ++(T)  --(T)  true(T)  false(T)</code>
    /// so implement processing those earlier.
    /// </remarks>
    public class CopyOperatorsToNamedRewriter : AbstractFullRewriter {

        readonly List<OperatorDeclarationSyntax> ops = new();

        string currentTypeName;

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
            ops.Clear();
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            return AddOps(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
            ops.Clear();
            currentTypeName = ((INamedTypeSymbol)CurrentSemantics.GetDeclaredSymbol(node)).ToString();
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            return AddOps(node);
        }

        SyntaxNode AddOps(TypeDeclarationSyntax node) {
            List<MethodDeclarationSyntax> newMethods = new();
            foreach(var op in ops) {
                string methodName = NameOperatorsCategory.GetMethodName(op);
                var methodDeclaration =
                    MethodDeclaration(
                        op.ReturnType, Identifier(methodName)
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
            }
            node = node.AddMembers(newMethods.ToArray());
            return node;
        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node) {
            ops.Add(node);
            return base.VisitOperatorDeclaration(node);
        }
    }
}
