using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler {
    internal readonly struct SyntaxSemanticsPair {
        public readonly SyntaxTree syntax;
        public readonly SemanticModel semantics;

        public SyntaxSemanticsPair(SyntaxTree syntaxTree, Compilation compilation) {
            syntax = syntaxTree;
            semantics = compilation.GetSemanticModel(syntaxTree);
        }
    }
}
