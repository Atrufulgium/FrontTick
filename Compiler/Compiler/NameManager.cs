using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Atrufulgium.FrontTick.Compiler {
    public class NameManager {
        // TODO: clean this shit up, again.
        // Also don't have a bunch of naming stuff in RegisterMethodsWalker
        // and CopyOperatorsToNamedRewriter.

        /// <summary>
        /// A dictionary converting fully qualified name c# => mcfunction name.
        /// </summary>
        readonly Dictionary<string, MCFunctionName> methodNames = new();
        /// <summary>
        /// The mcfunction namespace all functions live in.
        /// </summary>
        public readonly string manespace;
        /// <summary>
        /// The postprocessor for this instance.
        /// </summary>
        public readonly INameManagerPostProcessor postProcessor;
        /// <summary>
        /// All constants encountered, which all need initialisation in the
        /// datapack.
        /// </summary>
        readonly SortedSet<int> constants = new();

        /// <summary>
        /// The file name to put internal datapack setup into.
        /// </summary>
        public MCFunctionName SetupFileName => new($"{manespace}-internal:--load--");
        /// <summary>
        /// The file name to put test overview results into.
        /// </summary>
        public MCFunctionName TestPostProcessName => new($"{manespace}-internal:--test-postprocess--");

        public NameManager(string manespace, INameManagerPostProcessor postProcessor) {
            this.manespace = manespace;
            this.postProcessor = postProcessor;
        }

        /// <summary>
        /// Register a method declaration for future use. This returns whether
        /// the declaration results in a legal name.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool RegisterMethodname(
            SemanticModel semantics, 
            MethodDeclarationSyntax method, 
            string name, 
            ICustomDiagnosable diagnosticsOutput, 
            bool prefixNamespace = true,
            string fullyQualifiedName = null
        ) {
            if (fullyQualifiedName == null)
                fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);

            string path = name;
            if (prefixNamespace)
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
        /// One exception to this are the method's arguments, which lose their
        /// name and instead become <tt>#fully_qualified_method##arg0</tt>, etc.
        /// </para>
        /// <para>
        /// Another exception to this are variables <tt>#ALLCAPS</tt>, which
        /// instead return <tt>#ALLCAPS</tt> without full qualification.
        /// </para>
        /// </summary>
        /// <remarks>
        /// For this method to work, the containing context method must have
        /// been registered already.
        /// </remarks>
        private string GetVariableName(
            SemanticModel semantics,
            IdentifierNameSyntax identifier,
            ICustomDiagnosable diagnosticsOutput
        ) {
            string varName = GetVariableNameIgnoringInternals(semantics, identifier, diagnosticsOutput);
            var match = afterFinalPoundAllcapsRegex.Match(varName);
            if (match.Success) {
                // So apparantly c#'s regex API is hilariously weird -- a regex
                // `some (group)` that captures "some group" has a Groups
                // property ["some group", "group"], even though I only have
                // one capturing group. Ew.
                return match.Groups[1].Value;
            }
            return varName;
        }

        /// <summary>
        /// <inheritdoc cref="GetVariableName(SemanticModel, IdentifierNameSyntax, ICustomDiagnosable)"/>
        /// <para>
        /// Member accesses are transformed in the most direct way possible: a
        /// <tt>lorem.ipsum</tt> gets turned into <tt>#fully_qualified_method#lorem#ipsum</tt>.
        /// This is appended to the exceptions listed above, for instance
        /// <tt>#RET#x</tt> may occur.
        /// </para>
        /// </summary>
        private string GetVariableName(
            SemanticModel semantics,
            MemberAccessExpressionSyntax access,
            ICustomDiagnosable diagnosticsOutput
        ) {
            // Note that a.b.c is stored in the syntax tree as (a.b).c!
            // Also note that the two types are "simple" and "pointer".
            // lol pointers imagine that.
            List<string> accesses = new();
            while (access.Expression is MemberAccessExpressionSyntax a) {
                if (access.Kind() == SyntaxKind.PointerMemberAccessExpression)
                    diagnosticsOutput.AddCustomDiagnostic(DiagnosticRules.NoUnsafe, access);

                accesses.Add(access.Name.Identifier.Text);
                access = a;
            }
            accesses.Add(access.Name.Identifier.Text);
            if (access.Expression is not IdentifierNameSyntax identifier)
                throw new ArgumentException("Malformed access in the syntax tree!");

            string prefix = GetVariableName(semantics, identifier, diagnosticsOutput);
            // No datapack-normalisation necessary as ingame scoreboards handle like everything.
            string suffix = string.Join('#', ((IEnumerable<string>)accesses).Reverse().ToArray());
            return $"{prefix}#{suffix}";
        }

        /// <inheritdoc cref="GetVariableName(SemanticModel, MemberAccessExpressionSyntax, ICustomDiagnosable)"/>
        public string GetVariableName(
            SemanticModel semantics,
            ExpressionSyntax variable,
            ICustomDiagnosable diagnosticsOutput
        ) {
            if (variable is IdentifierNameSyntax id)
                return postProcessor.PostProcess(GetVariableName(semantics, id, diagnosticsOutput));
            else if (variable is MemberAccessExpressionSyntax member)
                return postProcessor.PostProcess(GetVariableName(semantics, member, diagnosticsOutput));
            throw CompilationException.ToDatapackVariableNamesAreFromIdentifiersOrAccesses;
        }

        public string GetCombinedName(string prefix, string suffix)
            => postProcessor.PostProcess($"{prefix}#{suffix}");

        /// <summary>
        /// This regex matches all strings ending in <tt>##ALLCAPS</tt>, and
        /// captures <tt>#ALLCAPS</tt> (without the first #).
        /// In particular, this matches:
        /// <code>
        ///   lorem#ipsum##ALLCAPS
        ///   ##EMPTYQUALIFIER       (this will never happen)
        /// </code>
        /// but not
        /// <code>
        ///   lorem#ipsum##nocaps
        ///   lorem#ipsum##
        ///   lorem#ipsum#ALLCAPS
        ///   lorem#ipsum##finalCAPS
        ///   #NOQUALIFIER
        /// </code>
        /// </summary>
        static readonly Regex afterFinalPoundAllcapsRegex
            = new(@"#(#[A-Z]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Does the same as
        /// <see cref="GetVariableName(SemanticModel, IdentifierNameSyntax, ICustomDiagnosable)"/>
        /// except for the <tt>#ALLCAPS</tt> variables part.
        /// </summary>
        private string GetVariableNameIgnoringInternals(
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
            return postProcessor.PostProcess($"#CONST#{value}");
        }

        public string GetArgumentName(MCFunctionName mcfunctionname, int index) {
            return postProcessor.PostProcess($"#{mcfunctionname}##arg{index}");
        }

        /// <summary>
        /// Gives the name of a unified return variable callees should store
        /// their results in, and callers should read the result form.
        /// </summary>
        public string GetRetName() => postProcessor.PostProcess("#RET");

        /// <summary>
        /// Gives the name of the label at the end of a method where the method
        /// returns its value.
        /// </summary>
        public static string GetRetGotoName() => "#ret-label";

        public static string GetGotoFlagName() => "#GOTOFLAG";

        /// <summary>
        /// Returns a string representation of all known constants in the
        /// datapack, tagged with their original value.
        /// </summary>
        public IEnumerable<(int, string)> GetAllConstantNames() {
            // Adding duplicates to a SortedSet counts as a modification to
            // the enumerator somewhy? Even though HashSet is fine.
            foreach (var constant in constants.EnumerateCopy())
                yield return (constant, GetConstName(constant));
        }

        /// <summary>
        /// Whether the given string is valid as the name of a function file.
        /// </summary>
        public static bool IsValidDatapackName(MCFunctionName name) {
            // There is exactly one : in there, but ignore :'s in general.
            string check = name.name.Replace(":", "");
            return check == NormalizeFunctionName(check);
        }

        /// <summary>
        /// This normalizes strings to the <c>[a-z0-9/._-]*</c> range normal
        /// datapack filenames support by lowercasing the letters, replacing
        /// spaces with underscores, replacing sharps by minuses, and
        /// discarding the rest. There is no check as to whether the result is
        /// sensible/unique!
        /// </summary>
        public static string NormalizeFunctionName(string str) {
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
                } else if (c == '#') {
                    builder.Append('-');
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
