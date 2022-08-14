using MCMirror;
using MCMirror.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// <summary>
        /// All constants encountered, which all need initialisation in the
        /// datapack.
        /// </summary>
        public ReadOnlyCollection<int> Constants => new(constants.ToArray());
        readonly SortedSet<int> constants = new();

        /// <summary>
        /// The file name to put internal datapack setup into.
        /// </summary>
        public MCFunctionName SetupFileName => new($"{manespace}-internal:--load--");
        /// <summary>
        /// The file name to put test overview results into.
        /// </summary>
        public MCFunctionName TestPostProcessName => new($"{manespace}-internal:--test-postprocess--");

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
            // Before doing the normal path, check first if it's a custom compiled method.
            // This is so hopelessly coupled with FindEntryPointsWalker.cs
            if (semantics.TryGetSemanticAttributeOfType(method, typeof(CustomCompiledAttribute), out var attrib)) {
                // No error checking this branch whatsoever because really, I'm me.
                string customName = (string)attrib.ConstructorArguments[0].Value;
                methodNames.Add(fullyQualifiedName, new MCFunctionName(customName));
                return true;
            }

            if (semantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out attrib)) {
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

        /// <inheritdoc cref="GetMethodName(SemanticModel, MethodDeclarationSyntax, ICustomDiagnosable, string)"/>
        public MCFunctionName GetMethodName(
            SemanticModel semantics,
            InvocationExpressionSyntax method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            string fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);
            return GetMethodName(fullyQualifiedName, method, diagnosticsOutput, scopeSuffix);
        }

        /// <inheritdoc cref="GetMethodName(SemanticModel, MethodDeclarationSyntax, ICustomDiagnosable, string)"/>
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
        /// <para>
        /// Transform a c# variable to a .mcfunction variable name of the form
        /// <tt>#fully_qualified_context#name</tt>, where the context can be
        /// for instance the fully qualified class name if it is a field, or
        /// a fully qualified method name if it is a local.
        /// </para>
        /// <para>
        /// The exception to this are the method's arguments, which lose their
        /// name and instead become <tt>#fully_qualified_method##arg0</tt>, etc.
        /// </para>
        /// </summary>
        /// <remarks>
        /// For this method to work, the containing context method must have
        /// been registered already.
        /// </remarks>
        public string GetVariableName(
            SemanticModel semantics,
            IdentifierNameSyntax identifier,
            ICustomDiagnosable diagnosticsOutput
        ) {
            var symbolInfo = semantics.GetSymbolInfo(identifier).Symbol;
            // This part is a bit ugly, but everything is just different enough
            // to require a bunch of annoying branches. The copypasta of those
            // two two lines is nicer than going through the effort to get it
            // outside.
            if (symbolInfo is IFieldSymbol fieldSymbol) {
                string context = NormalizeFunctionName(fieldSymbol.ContainingType.ToString());
                // *Want* to manually mirror the way MCFunctions look for some
                // consistency. Note that the context is always datapack-valid,
                // but the name after that can be anything.
                return $"#{manespace}:{context}#{fieldSymbol.Name}";
            } else if (symbolInfo is ILocalSymbol localSymbol) {
                string fullyQualifiedName = GetFullyQualifiedMethodName((IMethodSymbol)localSymbol.ContainingSymbol);
                MCFunctionName context = GetMethodName(fullyQualifiedName, identifier, diagnosticsOutput);
                return $"#{context}#{localSymbol.Name}";
            } else if (symbolInfo is IParameterSymbol paramSymbol) {
                string fullyQualifiedName = GetFullyQualifiedMethodName((IMethodSymbol)paramSymbol.ContainingSymbol);
                MCFunctionName context = GetMethodName(fullyQualifiedName, identifier, diagnosticsOutput);
                return GetArgumentName(context, paramSymbol.Ordinal);
            } else {
                throw CompilationException.ToDatapackVariablesFieldLocalOrParams;
            }
        }

        /// <summary>
        /// Transform a c# constant to a .mcfunction variable name of the form
        /// <tt>#CONST#value</tt>.
        /// </summary>
        /// <remarks>
        /// Once compilation is complete, don't forget to add a file that
        /// initializes all known constant values, found in
        /// <see cref="Constants"/>.
        /// </remarks>
        public string GetConstName(int value) {
            constants.Add(value);
            return $"#CONST#{value}";
        }

        public string GetArgumentName(MCFunctionName mcfunctionname, int index) {
            return $"#{mcfunctionname}##arg{index}";
        }

        /// <summary>
        /// Gives the name of a unified return variable callees should store
        /// their results in, and callers should read the result form.
        /// </summary>
        public static string GetRetName() => "#RET";

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
        public static string GetFullyQualifiedMethodName(SemanticModel semantics, MethodDeclarationSyntax method) {
            var methodModel = semantics.GetDeclaredSymbol(method);
            return GetFullyQualifiedMethodName(methodModel);
        }

        /// <inheritdoc cref="GetFullyQualifiedMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        public static string GetFullyQualifiedMethodName(SemanticModel semantics, InvocationExpressionSyntax method) {
            var methodModel = (IMethodSymbol)semantics.GetSymbolInfo(method).Symbol;
            return GetFullyQualifiedMethodName(methodModel);
        }

        /// <inheritdoc cref="GetFullyQualifiedMethodName(SemanticModel, MethodDeclarationSyntax)"/>
        public static string GetFullyQualifiedMethodName(IMethodSymbol method) {
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
    /// Outside code will have to use
    /// <see cref="NameManager.GetMethodName(SemanticModel, MethodDeclarationSyntax, ICustomDiagnosable, string)"/>
    /// (or overloads) to obtain instances.
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
