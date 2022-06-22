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
        public readonly SemanticModel model;
        public readonly MethodDeclarationSyntax method;
        public readonly string mcFunctionName;

        public EntryPoint(SemanticModel model, MethodDeclarationSyntax method, string mcFunctionName) {
            this.model = model;
            this.method = method;
            this.mcFunctionName = mcFunctionName;
        }

        public EntryPoint(SemanticModel model, MethodDeclarationSyntax method)
            : this(model, method, GetMCFunctionName(model, method)) { }

        public override bool Equals([NotNullWhen(true)] object obj) {
            if (obj is EntryPoint other)
                return model.Equals(other.model) && method.Equals(other.method);
            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(model, method);

        internal static string GetMCFunctionName(SemanticModel semantics, MethodDeclarationSyntax method) {
            string path;
            if (semantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out var attrib)) {
                if (attrib.ConstructorArguments.Length == 0)
                    path = semantics.GetFullyQualifiedMethodName(method);
                else
                    path = (string)attrib.ConstructorArguments[0].Value;
            } else {
                path = "internal/" + semantics.GetTypeInfo(method).ToString();
            }
            return DatapackFile.NormalizeFunctionName(path);
        }
    }
}
