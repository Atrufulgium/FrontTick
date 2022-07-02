using MCMirror;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler {
    public class NameManager {

        /// <summary>
        /// A dictionary converting fully qualified name c# => mcfunction name.
        /// </summary>
        readonly Dictionary<string, MCFunctionName> methodNames = new();
        /// <summary>
        /// The mcfunction namespace all functions live in.
        /// </summary>
        public readonly string manespace;

        public NameManager(string manespace) {
            this.manespace = manespace;
        }

        /// <summary>
        /// Register a method declaration for future use. This returns whether
        /// the declaration results in a legal name.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool RegisterMethodname(SemanticModel semantics, MethodDeclarationSyntax method, ICustomDiagnosable diagnosticsOutput) {
            string path;
            string fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);
            if (semantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out var attrib)) {
                if (attrib.ConstructorArguments.Length == 0) {
                    path = fullyQualifiedName;
                    path = NormalizeFunctionName(path);
                } else {
                    // Check whether the custom name is legal as mcfunction
                    path = (string)attrib.ConstructorArguments[0].Value;
                    if (path != NormalizeFunctionName(path) || path == "") {
                        diagnosticsOutput.AddCustomDiagnostic(
                            DiagnosticRules.MCFunctionAttributeIllegalName,
                            method,
                            method.Identifier.Text
                        );
                        return false;
                    }
                }
            } else {
                path = "internal/" + fullyQualifiedName;
                path = NormalizeFunctionName(path);
            }
            path = $"{manespace}:{path}";
            var mcFunctionName = new MCFunctionName(path);

            if (methodNames.TryGetValue(fullyQualifiedName, out MCFunctionName registeredPath)) {
                if (registeredPath != path)
                    throw new ArgumentException($"This method {fullyQualifiedName} is already registered with a different mcfunction name. That should not be possible.\n This:\n{path}\nExisting:\n{registeredPath}");
            }
            if (methodNames.ContainsValue(mcFunctionName)){ 
                // This is pretty rare so an expensive lookup doesn't matter.
                // If I cared I'd've used a bijective dictionary instead.
                string otherFullyQualifiedName = (
                    from keyvalue in methodNames
                    where keyvalue.Value == mcFunctionName
                    select keyvalue.Key
                    ).First();
                diagnosticsOutput.AddCustomDiagnostic(
                    DiagnosticRules.MCFunctionMethodNameClash,
                    method,
                    fullyQualifiedName, otherFullyQualifiedName, registeredPath
                );
                return false;
            }

            methodNames.Add(fullyQualifiedName, mcFunctionName);
            return true;
        }

        /// <summary>
        /// Get a registered method's mcfunction name, including namespace. If
        /// this is not found, it instead returns <tt>#UNKNOWN:#UNKNOWN</tt>.
        /// </summary>
        /// <param name="scopeSuffix">
        /// What to attach to the MCFunction name
        /// </param>
        public MCFunctionName GetMethodName(
            SemanticModel semantics,
            MethodDeclarationSyntax method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            string fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);
            return GetMethodName(fullyQualifiedName, method, diagnosticsOutput, scopeSuffix);
        }

        /// <inheritdoc cref="GetMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        public MCFunctionName GetMethodName(
            SemanticModel semantics,
            InvocationExpressionSyntax method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            string fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);
            return GetMethodName(fullyQualifiedName, method, diagnosticsOutput, scopeSuffix);
        }

        /// <inheritdoc cref="GetMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        private MCFunctionName GetMethodName(
            string fullyQualifiedName,
            SyntaxNode method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            if (!methodNames.TryGetValue(fullyQualifiedName, out MCFunctionName name)) {
                diagnosticsOutput.AddCustomDiagnostic(
                    DiagnosticRules.MCFunctionMethodNameNotRegistered,
                    method,
                    fullyQualifiedName
                );
                return new MCFunctionName("#UNKNOWN:#UNKNOWN");
            }
            if (scopeSuffix != "") {
                if (NormalizeFunctionName(scopeSuffix) != scopeSuffix)
                    throw new ArgumentException("The scope should also satisfy datapack naming rules.");
                return new MCFunctionName(name + scopeSuffix);
            }
            return name;
        }

        /// <summary>
        /// This normalizes strings to the <c>[a-z0-9/._-]*</c> range normal
        /// datapack filenames support by lowercasing the letters, replacing
        /// spaces with underscores, and discarding the rest. There is no check
        /// as to whether the result is sensible/unique!
        /// </summary>
        static string NormalizeFunctionName(string str) {
            StringBuilder builder = new(str.Length);
            foreach (char c in str) {
                if (('a' <= c && c <= 'z')
                    || ('0' <= c && c <= '9')
                    || c == '/' || c == '.'
                    || c == '_' || c == '-') {
                    builder.Append(c);
                } else if ('A' <= c && c <= 'Z') {
                    builder.Append((char)(c - 'A' + 'a'));
                } else if (c == ' ') {
                    builder.Append('_');
                }
                // Otherwise don't append anything and discard this char.
            }
            return builder.ToString();
        }

        /// <summary>
        /// Return the full name (namespace.class.method) of a method.
        /// </summary>
        static string GetFullyQualifiedMethodName(SemanticModel semantics, MethodDeclarationSyntax method) {
            var methodModel = semantics.GetDeclaredSymbol(method);
            return GetFullyQualifiedMethodName(methodModel);
        }

        /// <inheritdoc cref="GetFullyQualifiedMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        static string GetFullyQualifiedMethodName(SemanticModel semantics, InvocationExpressionSyntax method) {
            var methodModel = (IMethodSymbol)semantics.GetSymbolInfo(method).Symbol;
            return GetFullyQualifiedMethodName(methodModel);
        }

        /// <inheritdoc cref="GetFullyQualifiedMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        static string GetFullyQualifiedMethodName(IMethodSymbol method) {
            var containingType = method.ContainingType;
            string methodName = method.Name;
            string containingName = containingType.ToString();
            return $"{containingName}.{methodName}";
        }
    }

    /// <summary>
    /// A string that is specifically a mcfunction name; it is of the form
    /// <tt>namespace:class.method</tt> (without <tt>.mcfunction</tt>).
    /// </summary>
    /// <remarks>
    /// Do not instantiate this class somewhere other than the NameManager.
    /// </remarks>
    // Just "inherit" the string interfaces I can be bothered about.
    public class MCFunctionName : IEnumerable<char>, IEnumerable, IComparable, IEquatable<MCFunctionName>, ICloneable {
        /// <inheritdoc cref="MCFunctionName"/>
        public readonly string name;

        internal MCFunctionName(string name) {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            this.name = name;
        }

        public static implicit operator string(MCFunctionName name) => name.name;

        public override string ToString() => name;

        public int CompareTo(object obj) => name.CompareTo(obj is MCFunctionName str ? str.name : null);
        IEnumerator<char> IEnumerable<char>.GetEnumerator() => (name as IEnumerable<char>).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => (name as IEnumerable).GetEnumerator();
        public override bool Equals(object obj) => obj is MCFunctionName str && name.Equals(str.name);
        public override int GetHashCode() => name.GetHashCode();
        public bool Equals(MCFunctionName other) => name.Equals(other.name);
        object ICloneable.Clone() => new MCFunctionName((string)name.Clone());
        public static bool operator ==(MCFunctionName left, MCFunctionName right) => left.name == right.name;
        public static bool operator !=(MCFunctionName left, MCFunctionName right) => left.name != right.name;
    }
}
