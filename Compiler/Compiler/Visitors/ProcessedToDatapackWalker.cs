using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// A walker for turning the tree, fully processed into suitable form,
    /// into the actual datapack.
    /// </summary>
    /// <remarks>
    /// For a full description of this stage, see the file
    /// "<tt>./ProcessedToDatapackWalker.md</tt>".
    /// </remarks>
    public class ProcessedToDatapackWalker : AbstractFullWalker {

        public SortedSet<int> constants = new();

        readonly Stack<DatapackFile> wipFiles = new();
        readonly Stack<string> scopes = new();

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            var semantics = CurrentSemantics;
            MCFunctionName path = nameManager.GetMethodName(semantics, node, this);
            wipFiles.Push(new(path));
            scopes.Push(new((string)path));

            foreach(var statement in node.Body.Statements) {
                // If-soup copypasta-soup first, refactor later.
                if (statement is LocalDeclarationStatementSyntax decl) {
                    if (scopes.Count != 1)
                        throw CompilationException.ToDatapackDeclarationsMustBeInMethodRootScope;
                    foreach(var declarator in decl.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>())
                        if (declarator.Initializer != null)
                            throw CompilationException.ToDatapackDeclarationsMayNotBeInitializers;
                } else if (statement is ExpressionStatementSyntax expr) {
                    // Holy moly there's many
                    // https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Generated/CSharp.Generated.g4#L713
                    if (expr.Expression is AssignmentExpressionSyntax assign
                        && assign.Left is IdentifierNameSyntax lhs) {
                        string l = LocalVarName(lhs.Identifier.Text), r;
                        var op = assign.OperatorToken.Text;
                        if (Array.IndexOf(new[] { "=", "+=", "-=", "*=", "/=", "%=" }, op) < 0)
                            throw CompilationException.ToDatapackAssignmentOpsMustBeSimpleOrArithmetic;
                        bool isSetCommand = false;
                        if (assign.Right is LiteralExpressionSyntax rhsLit) {
                            // (todo: -x is not a literal lol)
                            isSetCommand = op == "=";
                            r = int.Parse(rhsLit.Token.Text).ToString(); // yes very wasteful, bleh, TODO: better exception
                            if (!isSetCommand)
                                r = ConstName(r);
                        } else if (assign.Right is IdentifierNameSyntax rhsId) {
                            r = LocalVarName(rhsId.Identifier.Text);
                        } else if (assign.Right is InvocationExpressionSyntax rhsCall) {
                            HandleInvocation(rhsCall);
                            r = "#RET";
                        } else {
                            throw CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls;
                        }
                        if (isSetCommand)
                            AddCode($"scoreboard players set {l} _ {r}");
                        else
                            AddCode($"scoreboard players operation {l} _ {op} {r} _");
                    } else if (expr.Expression is InvocationExpressionSyntax call) {
                        HandleInvocation(call);
                    }
                }
            }

            foreach (var file in wipFiles)
                compiler.finishedCompilation.Add(file);
        }

        private void HandleInvocation(InvocationExpressionSyntax call) {
            var method = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(call).Symbol;
            if (!method.IsStatic)
                throw CompilationException.ToDatapackMethodCallsMustBeStatic;

            string methodName = nameManager.GetMethodName(CurrentSemantics, call, this);
            // Currently ignoring in,out,ref.
            int i = 0;
            foreach(var arg in call.ArgumentList.Arguments) {
                string argName = LocalVarName($"arg{i}", methodName);
                // Too copypastay of the statement case, this code's the sketch anyway
                if (arg.Expression is LiteralExpressionSyntax lit) {
                    AddCode($"scoreboard players set {argName} _ {int.Parse(lit.Token.Text)}");
                } else if (arg.Expression is IdentifierNameSyntax id) {
                    AddCode($"scoreboard players operation {argName} _ = {LocalVarName(id.Identifier.Text)} _");
                } else {
                    throw CompilationException.ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals;
                }
                i++;
            }

            AddCode($"function {nameManager.manespace}:{methodName}");
        }

        private void AddCode(string code) => wipFiles.Peek().code.Add(code);
        private string LocalVarName(string name) => "#" + string.Join("#", scopes) + "#" + name;
        private static string LocalVarName(string name, string fullyQualifiedMethodName) => $"#{fullyQualifiedMethodName}##{name}";
        private string ConstName(string name) {
            constants.Add(int.Parse(name));
            return "#CONST#" + name;
        }
    }
}
