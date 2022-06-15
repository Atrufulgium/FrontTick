using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Represents an entrypoint for compilation to start.
    /// </summary>
    internal readonly struct EntryPoint {
        public readonly SyntaxSemanticsPair tree;
        public readonly MethodDeclarationSyntax method;

        public EntryPoint(SyntaxSemanticsPair tree, MethodDeclarationSyntax method) {
            this.tree = tree;
            this.method = method;
        }

        public override bool Equals([NotNullWhen(true)] object obj) {
            if (obj is EntryPoint other)
                return tree.Equals(other.tree) && method.Equals(other.method);
            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(tree, method);
    }
}
