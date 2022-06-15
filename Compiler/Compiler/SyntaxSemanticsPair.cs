using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    // I just like this mental model better than accessing the syntax tree
    // through semantics.SyntaxTree, they're on equal footing to me.
    internal readonly struct SyntaxSemanticsPair {
        public SyntaxTree syntax => semantics.SyntaxTree;
        public readonly SemanticModel semantics;

        public SyntaxSemanticsPair(SyntaxTree syntaxTree, Compilation compilation) {
            semantics = compilation.GetSemanticModel(syntaxTree);
        }
        public SyntaxSemanticsPair(SyntaxTree syntaxTree, SemanticModel semantics) {
            if (semantics.SyntaxTree != syntaxTree)
                throw new System.ArgumentException("The syntax tree and semantic model are unrelated.");
            this.semantics = semantics;
        }
        public SyntaxSemanticsPair(SemanticModel semantics) {
            this.semantics = semantics;
        }

        public override bool Equals([NotNullWhen(true)] object obj) {
            if (obj is SyntaxSemanticsPair other)
                return semantics.Equals(other.semantics);
            return false;
        }

        public override int GetHashCode()
            => semantics.GetHashCode();
    }
}
