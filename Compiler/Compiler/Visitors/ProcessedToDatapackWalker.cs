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
    // I "love" how this class is messy for the same reason every parser I've
    // ever written is messy, but the opposite way around.
    // Apologies for how interconnected all these methods are.
    public class ProcessedToDatapackWalker : AbstractFullWalker {

        /// <summary>
        /// <para>
        /// The files worked on, in order of encountering. The top is the
        /// current file being worked on. This is also a proxy for scoping --
        /// if this has just one element, we are at the root scope.
        /// </para>
        /// <para>
        /// It is the responsibility of the method that added to this, to also
        /// remove from this.
        /// </para>
        /// </summary>
        readonly Stack<DatapackFile> wipFiles = new();
        bool AtRootScope => wipFiles.Count == 1;

        MethodDeclarationSyntax currentNode;
        // Automatically incremented by
        /// <see cref="GetBranchPath(string)"/>
        // but needs manual resetting.
        int branchCounter;
        bool encounteredReturn;

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            currentNode = node;
            branchCounter = 0;
            encounteredReturn = false;
            // Don't do the base-call as we're manually walking everything from
            // here on out, as the code must abide a very specific structure.
            MCFunctionName path = nameManager.GetMethodName(CurrentSemantics, node, this);
            wipFiles.Push(new(path));

            HandleBlock(node.Body);

            while (wipFiles.Count > 0)
                compiler.finishedCompilation.Add(wipFiles.Pop());
        }

        private void HandleBlock(BlockSyntax block) {
            // Within if-else trees we only allow returns or branches.
            // Statements after returns get correctly flagged as wrong.
            // In order to flag statements *before* returns in branches as
            // "wrong", we need to check (for simplicity once at the end) if
            // this block contains both a return(/branch) and a non-return.
            // In that case, throw.
            // (Exception: the root scope allows returns and non-returns.)
            bool encounteredReturnIllegalStatement = false;

            foreach (var statement in block.Statements) {
                HandleStatement(statement);

                encounteredReturnIllegalStatement |= !(statement is ReturnStatementSyntax or IfStatementSyntax);
            }
            if (!AtRootScope && encounteredReturn && encounteredReturnIllegalStatement)
                throw CompilationException.ToDatapackReturnBranchMustBeReturnStatement;
        }

        // no blocks are not statements no matter what you try and tell me.
        private void HandleStatement(StatementSyntax statement) {
            // Group all returns separately to be able to check for the
            // condition "returns must be at the end".
            if (statement is ReturnStatementSyntax ret) {
                HandleReturn(ret);
                return;
            } else if (statement is IfStatementSyntax ifst) {
                HandleIfElseGroup(ifst);
                return;
            }

            if (encounteredReturn)
                throw CompilationException.ToDatapackReturnNoNonReturnAfterReturn;

            if (statement is LocalDeclarationStatementSyntax decl) {
                HandleLocalDeclaration(decl);
            } else if (statement is ExpressionStatementSyntax expr) {
                HandleExpression(expr.Expression);
            }
        }

        private void HandleBlockOrStatement(StatementSyntax statement) {
            if (statement is BlockSyntax block)
                HandleBlock(block);
            else
                HandleStatement(statement);
        }

        private void HandleLocalDeclaration(LocalDeclarationStatementSyntax decl) {
            if (!AtRootScope)
                throw CompilationException.ToDatapackDeclarationsMustBeInMethodRootScope;
            // Note that a declaration statement can consist of multiple
            // declarators, due to the syntax of `int i, j = 1, k;`.
            foreach (var declarator in decl.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>())
                if (declarator.Initializer != null)
                    throw CompilationException.ToDatapackDeclarationsMayNotBeInitializers;
        }

        private void HandleExpression(ExpressionSyntax expr) {
            // Holy moly there's many
            // https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Generated/CSharp.Generated.g4#L713
            if (expr is AssignmentExpressionSyntax assign
                && assign.Left is IdentifierNameSyntax lhs) {
                string lhsName = nameManager.GetVariableName(CurrentSemantics, lhs, this);
                HandleAssignment(assign, lhsName);
            } else if (expr is InvocationExpressionSyntax call) {
                HandleInvocation(call);
            }
        }

        private void HandleAssignment(AssignmentExpressionSyntax assign, string lhsName) {
            string rhsName;
            var op = assign.OperatorToken.Text;
            if (Array.IndexOf(new[] { "=", "+=", "-=", "*=", "/=", "%=" }, op) < 0)
                throw CompilationException.ToDatapackAssignmentOpsMustBeSimpleOrArithmetic;
            bool isSetCommand = false;

            // Partially copypasted into
            /// <see cref="HandleReturn(ReturnStatementSyntax)"/>
            if (TryGetIntegerLiteral(assign.Right, out int literal)) {
                isSetCommand = op == "=";
                rhsName = literal.ToString();
                if (!isSetCommand)
                    rhsName = nameManager.GetConstName(literal);
            } else if (assign.Right is IdentifierNameSyntax rhsId) {
                rhsName = nameManager.GetVariableName(CurrentSemantics, rhsId, this);
            } else if (assign.Right is InvocationExpressionSyntax rhsCall) {
                HandleInvocation(rhsCall);
                rhsName = NameManager.GetRetName();
            } else {
                throw CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls;
            }
            if (isSetCommand)
                AddCode($"scoreboard players set {lhsName} _ {rhsName}");
            else
                AddCode($"scoreboard players operation {lhsName} _ {op} {rhsName} _");
        }

        private void HandleIfElseGroup(IfStatementSyntax ifst) {
            MCFunctionName targetIfName = GetBranchPath("if-branch"), targetElseName = null;
            string conditionIdentifier;
            bool hasElse = ifst.Else != null;
            if (hasElse)
                targetElseName = GetBranchPath("else-branch");

            // The if-statement must be of the form
            //   if (identifier != 0)
            if (ifst.Condition is BinaryExpressionSyntax bin
                && bin.Left is IdentifierNameSyntax id
                && bin.OperatorToken.Text == "!="
                && TryGetIntegerLiteral(bin.Right, out int rhsValue)
                && rhsValue == 0) {
                conditionIdentifier = nameManager.GetVariableName(CurrentSemantics, id, this);
            } else {
                throw CompilationException.ToDatapackIfConditionalMustBeIdentifierNotEqualToZero;
            }

            // We need this among other things to check the condition of the
            // if-else tree at the end of the method consisting entirely of
            // return branches.
            bool returnedBeforeBranch = encounteredReturn;

            wipFiles.Push(new(targetIfName));
            HandleBlockOrStatement(ifst.Statement);
            // Don't do the extra file if it's just one command.
            if (wipFiles.Peek().code.Count == 1) {
                string command = wipFiles.Peek().code[0];
                wipFiles.Pop();
                AddCode($"execute unless score {conditionIdentifier} _ matches 0 run {command}");
            } else {
                compiler.finishedCompilation.Add(wipFiles.Pop());
                AddCode($"execute unless score {conditionIdentifier} _ matches 0 run function {targetIfName}");
            }

            bool returnedAtOrBeforeIf = encounteredReturn;

            if (hasElse) {
                // Same as above, but for else.
                // This stuff seems generalizable but if it remains these two
                // I'm not gonna bother.
                wipFiles.Push(new(targetElseName));
                HandleBlockOrStatement(ifst.Else.Statement);
                if (wipFiles.Peek().code.Count == 1) {
                    string command = wipFiles.Peek().code[0];
                    wipFiles.Pop();
                    AddCode($"execute if score {conditionIdentifier} _ matches 0 run {command}");
                } else {
                    compiler.finishedCompilation.Add(wipFiles.Pop());
                    AddCode($"execute if score {conditionIdentifier} _ matches 0 run function {targetElseName}");
                }
                if (!returnedAtOrBeforeIf && encounteredReturn) {
                    // Found a return statement within the else-branch, but
                    // there is not an if-branch. This is illegal.
                    throw CompilationException.ToDatapackReturnElseMustAlsoHaveReturnIf;
                }
            } else if (!returnedBeforeBranch && returnedAtOrBeforeIf) {
                // Found a return statement within the if-branch, but there is
                // not an else-branch. This is illegal.
                throw CompilationException.ToDatapackReturnIfMustAlsoHaveReturnElse;
            }
        }

        private void HandleInvocation(InvocationExpressionSyntax call) {
            var method = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(call).Symbol;
            if (!method.IsStatic)
                throw CompilationException.ToDatapackMethodCallsMustBeStatic;

            MCFunctionName methodName = nameManager.GetMethodName(CurrentSemantics, call, this);
            // TODO: Currently ignoring in,out,ref.
            int i = 0;
            foreach(var arg in call.ArgumentList.Arguments) {
                string argName = nameManager.GetArgumentName(methodName, i);
                // Too copypastay of the statement case, this code's the sketch anyway
                if (TryGetIntegerLiteral(arg.Expression, out int literal)) {
                    AddCode($"scoreboard players set {argName} _ {literal}");
                } else if (arg.Expression is IdentifierNameSyntax id) {
                    AddCode($"scoreboard players operation {argName} _ = {nameManager.GetVariableName(CurrentSemantics, id, this)} _");
                } else {
                    throw CompilationException.ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals;
                }
                i++;
            }

            AddCode($"function {methodName}");
        }

        private void HandleReturn(ReturnStatementSyntax ret) {
            encounteredReturn = true;
            // This is basically an assignment "#RET = [return content]".
            // Currently basically copypasta from
            /// <see cref="HandleAssignment(AssignmentExpressionSyntax, string)"/>
            // but should be refactored to be neater at some point.
            // OTOH, it's subtly different *enough* with the call case.
            string RET = NameManager.GetRetName();
            if (TryGetIntegerLiteral(ret.Expression, out int literal)) {
                AddCode($"scoreboard players set {RET} _ {literal}");
            } else if (ret.Expression is IdentifierNameSyntax id) {
                string identifier = nameManager.GetVariableName(CurrentSemantics, id, this);
                AddCode($"scoreboard players operation {RET} _ = {identifier} _");
            } else if (ret.Expression is InvocationExpressionSyntax call) {
                HandleInvocation(call);
                // No need to assign `call`'s #RET result to our (the same)
                // #RET lol
            } else {
                throw CompilationException.ToDatapackReturnMustBeIdentifierOrLiteralsOrCalls;
            }
        }

        /// <summary>
        /// This returns a MCFunction to uniquely identify a branch in the
        /// code. There will be both a human-readable identifier, and after
        /// that, a counter to ensure uniqueness. This results in a function
        /// name of the form
        /// <tt>namespace:class.method-counter-identifier</tt>.
        /// </summary>
        private MCFunctionName GetBranchPath(string identifier) {
            MCFunctionName ret = nameManager.GetMethodName(CurrentSemantics, currentNode, this, $"-{branchCounter}-{identifier}");
            branchCounter++;
            return ret;
        }

        /// <summary>
        /// Try to turn an expression that is supposed to represent a integer
        /// literal into the actual number it represents.
        /// Returns whether the ExpressionSyntax is of the correct type.
        /// If so, it can either throw in unsupported syntaxes, or populate the
        /// out variable with the actual value.
        /// </summary>
        private static bool TryGetIntegerLiteral(ExpressionSyntax expr, out int value) {
            if (expr is PrefixUnaryExpressionSyntax unary) {
                return TryGetIntegerLiteral(unary, out value);
            } else if (expr is LiteralExpressionSyntax lit) {
                value = GetIntegerLiteral(lit);
                return true;
            }
            value = 0;
            return false;
        }
        // Only call this from TryGetIntegerLiteral
        private static bool TryGetIntegerLiteral(PrefixUnaryExpressionSyntax unary, out int value) {
            if (!TryGetIntegerLiteral(unary.Operand, out value))
                return false;

            if (unary.Kind() == SyntaxKind.UnaryPlusExpression)
                return true;
            else if (unary.Kind() == SyntaxKind.UnaryMinusExpression) {
                value = -value;
                return true;
            }
            throw CompilationException.ToDatapackUnsupportedUnary;
        }
        // Only call this from TryGetIntegerLiteral
        private static int GetIntegerLiteral(LiteralExpressionSyntax lit) {
            if (!int.TryParse(lit.Token.Text, out int res))
                throw CompilationException.ToDatapackLiteralsIntegerOnly;
            return res;
        }

        /// <summary>
        /// Adds code to the currently considered datapack file.
        /// </summary>
        /// <remarks>
        /// Because it's such an easy optimisation, it replaces chained
        /// execute commands of the form `execute ... run execute ...` to not
        /// have the intermediate `run execute` and just chain it directly.
        /// </remarks>
        private void AddCode(string code)
            => wipFiles.Peek().code.Add(code.Replace(" run execute ", " "));
    }
}
