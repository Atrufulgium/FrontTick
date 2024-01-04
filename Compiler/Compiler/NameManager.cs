using Atrufulgium.FrontTick.Compiler.Datapack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler {
    public class NameManager {
        // TODO: clean this shit up, again.
        // Also don't have a bunch of naming stuff in RegisterMethodsWalker
        // and CopyOperatorsToNamedRewriter.

        /// <summary>
        /// A dictionary converting a method's data => mcfunction name.
        /// </summary>
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality", Justification = "See the comment in the SymbolNameComparer's source.")]
        readonly Dictionary<IMethodSymbol, MCFunctionName> methodNames = new(new SymbolNameComparer());
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
        public MCFunctionName SetupFileName => new($"{manespace}:internal/--load--");
        /// <summary>
        /// The file name to put test overview results into.
        /// </summary>
        public MCFunctionName TestPostProcessName => new($"{manespace}:internal/--test-postprocess--");

        public NameManager(string manespace, INameManagerPostProcessor postProcessor) {
            this.manespace = manespace;
            this.postProcessor = postProcessor;
        }

        /// <summary>
        /// <para>
        /// Register a method declaration for future use. This returns whether
        /// the declaration results in a legal name.
        /// </para>
        /// <para>
        /// When <paramref name="name"/> is null, the method name will be the
        /// fully qualified name. Otherwise, it's its value.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// This does not work for modified trees as it uses semantics. This
        /// means that using it in Rewriters is mostly questionable, but in
        /// Walkers it's perfectly fine.
        /// </para>
        /// </remarks>
        public bool RegisterMethodname(
            SemanticModel semantics, 
            MethodDeclarationSyntax method, 
            ICustomDiagnosable diagnosticsOutput, 
            string name = null,
            bool isInternal = false,
            bool prefixNamespace = true,
            bool suffixParams = true
        ) {
            string fullyQualifiedName = GetFullyQualifiedMethodName(semantics, method);

            string path = name ?? fullyQualifiedName;
            if (isInternal)
                path = $"internal/{NormalizeFunctionName(path)}";
            if (prefixNamespace)
                path = $"{manespace}:{NormalizeFunctionName(path)}";

            // To support method overloading, simply add for every param
            // its type to the call name.
            // You may also overload via "no modifier" and "any of ref/in/out"
            // so `-mod-param` would be sufficient, but eh. This is nicer.
            var methodSymbol = semantics.GetDeclaredSymbol(method);
            if (suffixParams) {
                foreach (var param in methodSymbol.Parameters) {
                    switch (param.RefKind) {
                        case RefKind.Ref:
                            path += "-ref";
                            break;
                        case RefKind.Out:
                            path += "-out";
                            break;
                        case RefKind.In:
                            path += "-in";
                            break;
                    }
                    path += $"-{NormalizeFunctionName(param.Type.Name)}";
                }
            }

            var mcFunctionName = new MCFunctionName(postProcessor.PostProcessFunction(path));

            if (methodNames.TryGetValue(methodSymbol, out MCFunctionName registeredPath)) {
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
                    ).First().Name;
                diagnosticsOutput.AddCustomDiagnostic(
                    DiagnosticRules.MCFunctionMethodNameClash,
                    method,
                    fullyQualifiedName, otherFullyQualifiedName, registeredPath
                );
                return false;
            }

            methodNames.Add(methodSymbol, mcFunctionName);
            return true;
        }

        /// <summary>
        /// <para>
        /// Get a registered method's mcfunction name, including namespace. If
        /// this is not found, it instead returns <tt>#UNKNOWN:#UNKNOWN</tt>.
        /// </para>
        /// </summary>
        /// <param name="scopeSuffix">
        /// What to attach to the MCFunction name
        /// </param>
        public MCFunctionName GetMethodName(
            SemanticModel semantics,
            SyntaxNode method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            var methodSymbol = (IMethodSymbol)semantics.GetSymbolInfo(method).Symbol;
            methodSymbol ??= (IMethodSymbol)semantics.GetDeclaredSymbol(method);
            if (!methodNames.TryGetValue(methodSymbol, out MCFunctionName name)) {
                diagnosticsOutput.AddCustomDiagnostic(
                    DiagnosticRules.MCFunctionMethodNameNotRegistered,
                    method,
                    methodSymbol.ToString()
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

        public bool MethodNameIsRegistered(SemanticModel semantics, SyntaxNode method) {
            var methodSymbol = (IMethodSymbol)semantics.GetSymbolInfo(method).Symbol;
            methodSymbol ??= (IMethodSymbol)semantics.GetDeclaredSymbol(method);
            return methodNames.ContainsKey(methodSymbol);
        }

        /// <summary>
        /// <inheritdoc cref="GetMethodName(SemanticModel, SyntaxNode, ICustomDiagnosable, string)"/>
        /// <para>
        /// TODO: This method is a bunch slower because we call the other
        /// overload by recovering the syntax node. This can be a lot better
        /// but I don't care currently.
        /// </para>
        /// </summary>
        public MCFunctionName GetMethodName(
            SemanticModel semantics,
            IMethodSymbol method,
            ICustomDiagnosable diagnosticsOutput,
            string scopeSuffix = ""
        ) {
            SyntaxNode methodNode = method.DeclaringSyntaxReferences.First().GetSyntax();
            return GetMethodName(semantics, methodNode, diagnosticsOutput, scopeSuffix);
        }

        /// <summary>
        /// <para>
        /// Transform a c# variable to a .mcfunction variable name of the form
        /// <tt>#datapack-namespace:fully_qualified_context#name</tt>, where
        /// the context can be for instance the fully qualified class name if
        /// it is a field, or a fully qualified method name if it is a local.
        /// </para>
        /// <para>
        /// One exception to this are the method's arguments, which lose their
        /// name and instead become <tt>#fully_qualified_method##arg0</tt>, etc.
        /// </para>
        /// <para>
        /// Another exception to this are local variables <tt>#ALLCAPS</tt>,
        /// which instead return <tt>#ALLCAPS</tt> without full qualification.
        /// </para>
        /// <para>
        /// Member accesses are transformed in the most direct way possible: a
        /// <tt>lorem.ipsum</tt> gets turned into <tt>#fully_qualified_method#lorem#ipsum</tt>.
        /// This is appended to the exceptions listed above, for instance
        /// <tt>#RET#x</tt> may occur.
        /// </para>
        /// </summary>
        /// <remarks>
        /// For this method to work, the containing context method, if any,
        /// must have been registered already.
        /// </remarks>
        public string GetVariableName(
            SemanticModel semantics,
            ExpressionSyntax node,
            ICustomDiagnosable diagnosticsOutput
        ) {
            bool isMethodLocal = false;

            if (!(node is IdentifierNameSyntax or MemberAccessExpressionSyntax))
                throw CompilationException.ToDatapackVariableNamesAreFromIdentifiersOrAccesses;
            // Format: datapack-namespace:Fully.Qualified.TypeOrMethod#var#sub#part
            // For consistency with method handling, the prefix component parts
            // need to be mangled by NormalizeFunctionName.

            // Store front to back fine to coarse.
            LinkedList<ISymbol> symbols = new();
            ExpressionSyntax currentPart = node;
            // Note that a.b.c.d is stored in the syntax tree as ((a.b).c).d,
            // so we're working from fine-grained fields/locals/etc up to
            // coarse classes/namespaces/etc.
            while (true) {
                var symbol = semantics.GetSymbolInfo(currentPart).Symbol;
                symbols.AddLast(symbol);
                if (symbol is ILocalSymbol)
                    isMethodLocal = true;

                if (currentPart is MemberAccessExpressionSyntax mae) {
                    currentPart = mae.Expression;
                } else {
                    break;
                }
            }

            /* Now we may have ended the chain too early. Expand it.
               (E.g. this was called on a `Class.member` instead of
               `Namespace.Class.member`, or `functionLocal.part` instead of
               `NameSpace.Class.Method.functionLocal.part`.)
               Symbols going into the prefix:
               - ITypeSymbol
                 (This includes IArrayTypeSymbol, ITypeParameterSymbol, which
                  will be supported later.)
                 (This includes IDynamicTypeSymbol, IErrorTypeSymbol,
                  IFunctionPointerTypeSymbol, IPointerTypeSymbol, which will
                  not be supported and should've thrown by now.)
               - INamespaceSymbol
               - IMethodSymbol (note: not actually in user written qualifications)
               which are conveniently enclosed in `INamespaceOrTypeSymbol`.
               Symbols going into the suffix:
               - IFieldSymbol
               - ILocalSymbol
               - IParameterSymbol (which have custom names)
               Other symbols are ignored. These are:
               - Due to irrelevancy: IDiscardSymbol, ILabelSymbol, IPreprocessingSymbol, IRangeVariableSymbol
               - Due to being rewritten: IPropertySymbol, IEventSymbol in the future
               - By choise: IAssemblySymbol (→ISourceAssemblySymbol), IModuleSymbol
               - IAliasSymbol (for using aliases, relevant but unsupported)
            */
            while (true) {
                var lastSymbol = symbols.Last.Value;
                // Find the first supported containing symbol
                var parentSymbol = lastSymbol;
                while (true) {
                    parentSymbol = parentSymbol.ContainingSymbol;
                    if (parentSymbol == null)
                        break;
                    // Note handle the global namespace symbol separately.
                    if (parentSymbol is ITypeSymbol
                        or IMethodSymbol
                        or IFieldSymbol or ILocalSymbol or IParameterSymbol)
                        break;
                    if (parentSymbol is INamespaceSymbol ns && !ns.IsGlobalNamespace)
                        break;
                }
                if (parentSymbol == null)
                    break;
                symbols.AddLast(parentSymbol);
            }

            // "symbols" now contains the full qualification.
            // The first is the most fine-grained. The last is the most coarse
            // grained (and probably the global namespace).
            // First, the local part. These are delimited by "#".
            string suffix = "";
            while (!(symbols.First.Value is INamespaceOrTypeSymbol or IMethodSymbol)) {
                var symbol = symbols.PopFirst();
                string add;
                if (symbol is IParameterSymbol param) {
                    add = $"#arg{param.Ordinal}";
                } else if (symbol is IFieldSymbol or ILocalSymbol) {
                    add = symbol.Name;
                } else {
                    throw new InvalidOperationException("Aught-to-be-impossible branch.");
                }
                if (suffix == "")
                    suffix = add;
                else
                    suffix = add + "#" + suffix;
            }

            // Handle the exception of "#ALLCAPS" locals here.
            if (isMethodLocal) {
                var parts = suffix.Split('#');
                if (parts.Length >= 2 && parts[0] == "" && parts[1] == parts[1].ToUpperInvariant())
                    return postProcessor.PostProcessVariable(suffix);
            }

            // Now we're at the broad part. These are delimited by ".".
            string prefix = "";
            while (true) {
                var symbol = symbols.PopFirst();
                string add;
                bool didMethodSymbol = false;
                if (symbol is INamespaceOrTypeSymbol) {
                    // Note: Primitives' names in method registration etc are
                    // the shorthands. As such, using `symbol.Name` here is
                    // fine.
                    add = symbol.Name;
                    add = NormalizeFunctionName(add);
                } else if (symbol is IMethodSymbol method) {
                    // Methods have special handling!
                    // (Also they don't need normalization as method
                    //  registration does that already.)
                    didMethodSymbol = true;
                    add = GetMethodName(semantics, method, diagnosticsOutput);
                } else {
                    throw new InvalidOperationException("Aught-to-be-impossible branch.");
                }
                if (prefix == "")
                    prefix = add;
                else
                    prefix = add + "." + prefix;

                // Grabbing the namespace:
                // - If we were a method symbol we are done because GetMethodName
                //   does all the work for us.
                // - If we are the final symbol, we need to grab the namespace
                //   manually.
                // Otherwise, there's still stuff to do and don't grab it.
                if (didMethodSymbol) {
                    prefix = $"#{prefix}";
                    break;
                }
                if (symbols.Count == 0) {
                    prefix = $"#{manespace}:{prefix}";
                    break;
                }
            }
            
            return postProcessor.PostProcessVariable($"{prefix}#{suffix}");
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
            return postProcessor.PostProcessVariable($"#CONST#{value}");
        }

        // TODO: Also handle this as just VariableName.
        public string GetArgumentName(MCFunctionName mcfunctionname, int index) {
            return postProcessor.PostProcessVariable($"#{mcfunctionname}##arg{index}");
        }

        // TODO: Properly go through each of these variable names and whether
        // or not they need processing here or elsewhere.
        /// <summary>
        /// Gives the name of a unified return variable callees should store
        /// their results in, and callers should read the result form.
        /// </summary>
        public string GetRetName() => "#RET";

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
        public static bool IsValidDatapackName(string name) {
            // There is exactly one : in there, but ignore :'s in general.
            string check = name.Replace(":", "");
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
            this.name = name ?? throw new ArgumentNullException(nameof(name));
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
