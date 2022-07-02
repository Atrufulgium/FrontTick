using MCMirror;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Represents an entrypoint for compilation to start.
    /// </summary>
    public readonly struct EntryPoint {
        public readonly SemanticModel semantics;
        public readonly MethodDeclarationSyntax method;

        public EntryPoint(SemanticModel semantics, MethodDeclarationSyntax method) {
            this.semantics = semantics;
            this.method = method;
        }

        public override bool Equals([NotNullWhen(true)] object obj) {
            if (obj is EntryPoint other)
                return semantics.Equals(other.semantics) && method.Equals(other.method);
            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(semantics, method);
    }
}
