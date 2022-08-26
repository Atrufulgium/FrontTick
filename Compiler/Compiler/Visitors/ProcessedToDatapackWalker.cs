using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using MCMirror;

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
    public class ProcessedToDatapackWalker : AbstractFullWalker<SetupCategory, PreProcessCategory, GotoLabelerWalker> {

        private GotoLabelerWalker GotoLabelerWalker => Dependency3;

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
        // We're at root scope if we aren't in a branch.
        // However, gotos can introduce wipFiles.
        // As such, we're only at root scope if no wipFiles are branchy.
        bool AtRootScope => wipFiles.All(
            file => !file.Path.name.Contains("-if-")
                && !file.Path.name.Contains("-else-")
        );

        MethodDeclarationSyntax currentNode;
        // Automatically incremented by
        /// <see cref="GetBranchPath(string)"/>
        // but needs manual resetting.
        int branchCounter;
        bool encounteredReturn;
        Dictionary<int, MCFunctionName> gotoFunctionNames;

        /// <summary>
        /// All methods with a [MCTest(int)] attribute. To be collected to be
        /// put into a minecraft function tag at a later stage.
        /// </summary>
        public List<MCFunctionName> testFunctions = new();

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            currentNode = node;
            branchCounter = 0;
            encounteredReturn = false;
            gotoFunctionNames = new();
            // Don't do the base-call as we're manually walking everything from
            // here on out, as the code must abide a very specific structure.
            MCFunctionName path = nameManager.GetMethodName(CurrentSemantics, node, this);
            wipFiles.Push(new(path));

            HandleBlock(node.Body);

            // If this method is a test, we need to add some post processing to
            // the mcfunction. As such, pop all but the last, do that
            // processing, and then finish.
            while (wipFiles.Count > 1)
                compiler.finishedCompilation.Add(wipFiles.Pop());

            if (CurrentSemantics.TryGetSemanticAttributeOfType(node, typeof(MCTestAttribute), out var attrib))
                HandleMCTestMethod(node, attrib);

            if (wipFiles.Count == 1)
                compiler.finishedCompilation.Add(wipFiles.Pop());
        }

        private void HandleBlock(BlockSyntax block) {
            // Within if-else trees we only allow returns or branches.
            // Statements after returns get correctly flagged as wrong.
            // In order to flag statements *before* returns in branches as
            // "wrong", we need to check (for simplicity once at the end) if
            // this block contains both a return(/branch) and a non-return.
            // In that case, throw.
            // (Exception: the root scope allows returns and non-returns.
            //  Also, take into account labels!)
            bool encounteredReturnIllegalStatement = false;
            bool encounteredGoto = false;

            foreach (var statement in block.Statements) {
                // (Don't want to consider labels in the return-legality, so
                //  extract the statement. Labels may nest.)
                var checkStatement = statement;
                while (checkStatement is LabeledStatementSyntax labeled) {
                    checkStatement = labeled.Statement;
                    encounteredGoto = false;
                }
                encounteredReturnIllegalStatement |= !(checkStatement is ReturnStatementSyntax or IfStatementSyntax);
                // Only labels may follow gotos.
                // Deliberately checking `statement` and not `checkStatement`.
                if (encounteredGoto && statement is not LabeledStatementSyntax)
                    throw CompilationException.ToDatapackGotoMustBeLastBlockStatement;
                encounteredGoto |= checkStatement is GotoStatementSyntax;

                HandleStatement(statement);
            }
            if (!AtRootScope && encounteredReturn && encounteredReturnIllegalStatement)
                throw CompilationException.ToDatapackReturnBranchMustBeReturnStatement;
        }

        private void HandleStatement(StatementSyntax statement) {
            // Labels are ugly nested statements. Get rid of them.
            // Fun fact: the things *can be nested*. Somewhy, someone decided
            //     `label1: label2: label3: label4: return "ew";`
            // is valid c#. Thanks, someone.
            while (statement is LabeledStatementSyntax labeled) {
                HandleGotoLabel(labeled, innerStatement: out statement);
            }

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
            } else if (statement is GotoStatementSyntax got) {
                HandleGoto(got);
            } else if (statement is EmptyStatementSyntax) {
                // We'll also autogen this a ton, probably.
            } else {
                throw CompilationException.ToDatapackUnsupportedStatementType;
            }
            // Note that blocks are *not* checked here. This is on purpose, as
            // not having nested blocks for no reason is a nice assumption.
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
            // TODO: Implementing goto in here is kinda messy and really
            // deserves its own pass. Nevertheless, for the time being, I'll
            // put the spaghetti here.
            // For how to transform, see the comments of:
            /// <see cref="GotoLabelerWalker.AfterScopeRequiresFlag(BlockSyntax)"/>
            /// <see cref="GotoLabelerWalker.AfterScopeRequiresFlagConsume(BlockSyntax, out IEnumerable{int})"/>
            MCFunctionName targetIfName = GetBranchPath("if-branch"),
                targetElseName = null,
                gotoContinuation;
            string conditionIdentifier;
            bool hasElse = ifst.Else != null;
            if (hasElse)
                targetElseName = GetBranchPath("else-branch");

            // The if-statement must be of the form
            //   if (identifier != literal) [or ==]
            if (ifst.Condition is BinaryExpressionSyntax bin
                && bin.Left is IdentifierNameSyntax id
                && bin.OperatorToken.Text is "!=" or "=="
                && TryGetIntegerLiteral(bin.Right, out int rhsValue)) {
                conditionIdentifier = nameManager.GetVariableName(CurrentSemantics, id, this);
            } else {
                throw CompilationException.ToDatapackIfConditionalMustBeIdentifierNotEqualToZero;
            }

            // In case we have an else branch, copy the if conditional over to
            // the else conditional to ensure that not both branches run if
            // the conditioned variable gets updated.
            // This is only necessary when there's a chance it's written to.
            if (hasElse) {
                var dataFlow = CurrentSemantics.AnalyzeDataFlow(ifst);
                var identifierSymbol = CurrentSemantics.GetSymbolInfo(id).Symbol;
                bool modifiesIdentifier = 
                    dataFlow.WrittenInside.Contains(identifierSymbol)
                    || !(identifierSymbol is ILocalSymbol or IParameterSymbol);

                if (modifiesIdentifier) {
                    // Use `branchCounter` to ensure uniqueness.
                    string updatedIdentifier = $"conditionIdentifier-{branchCounter}";
                    AddCode($"scoreboard players operation {updatedIdentifier} _ = {conditionIdentifier} _");
                    conditionIdentifier = updatedIdentifier;
                }
            }

            bool equalsVariant = bin.OperatorToken.Text == "==";
            string ifBranchMCConditional = equalsVariant ? "if" : "unless";
            string elseBranchMCConditional = equalsVariant ? "unless" : "if";

            // We need this among other things to check the condition of the
            // if-else tree at the end of the method consisting entirely of
            // return branches.
            bool returnedBeforeBranch = encounteredReturn;

            var parentBlock = ifst.Ancestors().OfType<BlockSyntax>().First();
            bool requireFlag = GotoLabelerWalker.AfterScopeRequiresFlag(parentBlock);
            bool requireFlagConsume = GotoLabelerWalker.AfterScopeRequiresFlagConsume(parentBlock, out var consumed);

            wipFiles.Push(new(targetIfName));
            if (ifst.Statement is BlockSyntax block)
                HandleBlock(block);
            else
                throw CompilationException.ToDatapackBranchesMustBeBlocks;
            // The above handling of blocks can introduce more wipFiles (via
            // gotos and such).
            // Every such file will be fully processed and done by the time we
            // return here. As such, we need to pop until we're back at the if-
            // block that brought us here in the first place.
            while (wipFiles.Peek().Path != targetIfName)
                compiler.finishedCompilation.Add(wipFiles.Pop());

            // Don't do the extra file if it's just one command.
            if (wipFiles.Peek().code.Count == 1) {
                string command = wipFiles.Peek().code[0];
                wipFiles.Pop();
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {command}");
            } else {
                compiler.finishedCompilation.Add(wipFiles.Pop());
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run function {targetIfName}");
            }

            bool returnedAtOrBeforeIf = encounteredReturn;

            if (hasElse) {
                // Same as above, but for else.
                // This stuff seems generalizable but if it remains these two
                // I'm not gonna bother.
                if (ifst.Else.Statement is not BlockSyntax elseBlock)
                    throw CompilationException.ToDatapackBranchesMustBeBlocks;
                requireFlag |= GotoLabelerWalker.AfterScopeRequiresFlag(elseBlock);
                requireFlagConsume |= GotoLabelerWalker.AfterScopeRequiresFlagConsume(elseBlock, out var elseConsumed);
                consumed = consumed.Union(elseConsumed);

                wipFiles.Push(new(targetElseName));
                HandleBlock(elseBlock);
                while (wipFiles.Peek().Path != targetElseName)
                    compiler.finishedCompilation.Add(wipFiles.Pop());

                if (wipFiles.Peek().code.Count == 1) {
                    string command = wipFiles.Peek().code[0];
                    wipFiles.Pop();
                    AddCode($"execute {elseBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {command}");
                } else {
                    compiler.finishedCompilation.Add(wipFiles.Pop());
                    AddCode($"execute {elseBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run function {targetElseName}");
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

            if (requireFlag || requireFlagConsume) {
                gotoContinuation = GetBranchPath("goto-continuation");
                bool continuationIsSimple = consumed.Count() == 1;
                // If `requireFlag`, no matter `requireFlagConsume`, we add a
                // "if (no flag set) { /* Rest of the code */ }".
                // If `requireFlagConsume`, but not `requireFlag`, we add a
                // "if (no current scope flag set) { /* Rest of the code */ }".
                // If `requireFlagConsume`, in addition to the above we add
                // "if (some current scope flag set) { reset flag; goto flag's label }".
                if (requireFlag)
                    AddCode($"execute if score #GOTOFLAG _ matches 0 run function {gotoContinuation}");
                else if (requireFlagConsume) {
                    if (continuationIsSimple) {
                        AddCode($"execute unless score #GOTOFLAG _ matches {consumed.First()} run function {gotoContinuation}");
                    } else {
                        foreach (int flag in consumed)
                            AddCode($"execute if score #GOTOFLAG _ matches {flag} run scoreboard players set #FLAGFOUND _ 1");
                        AddCode($"execute unless score #FLAGFOUND _ matches 1 run function {gotoContinuation}");
                        AddCode($"scoreboard players set #FLAGFOUND _ 0");
                    }
                }
                if (requireFlagConsume) {
                    foreach (int flag in consumed) {
                        // Go to a small intermediate function that resets the flag,
                        // and then run the goto.
                        var gotoBranch = GetBranchPath($"goto-{flag}");
                        var gotoLabel = GetGotoFunctionName(flag);
                        AddCode($"execute if score #GOTOFLAG _ matches {flag} run function {gotoBranch}");
                        wipFiles.Push(new(gotoBranch));
                        AddCode($"scoreboard players set #GOTOFLAG _ 0");
                        AddCode($"function {gotoLabel}");
                        compiler.finishedCompilation.Add(wipFiles.Pop());
                    }
                }
                // Now put the original rest of the file after the continuation
                wipFiles.Push(new(gotoContinuation));
            }
        }

        private void HandleGoto(GotoStatementSyntax got) {
            // Just set a flag and let the if-else tree handle the rest.
            // However, if there is no if-else tree, (the goto is on the same
            // level as the label,) immediately go.
            var parentBlock = got.Ancestors().OfType<BlockSyntax>().First();
            string name = got.Target();
            int id = GotoLabelerWalker.LabelToInt(name);
            if (GotoLabelerWalker.ScopeContainsLabel(parentBlock, name)) {
                var gotoLabel = GetGotoFunctionName(id);
                AddCode($"function {gotoLabel}");
            } else {
                AddCode($"scoreboard players set #GOTOFLAG _ {id}");
            }
        }

        private void HandleGotoLabel(LabeledStatementSyntax label, out StatementSyntax innerStatement) {
            string name = label.Identifier.Text;
            var gotoBranch = GetGotoFunctionName(GotoLabelerWalker.LabelToInt(name));
            AddCode($"function {gotoBranch}");
            wipFiles.Push(new(gotoBranch));
            // Do not handle the statement here -- instead, return control to HandleStatement
            // to let it work on the innerStatement.
            innerStatement = label.Statement;
        }

        private void HandleInvocation(InvocationExpressionSyntax call) {
            var method = (IMethodSymbol)CurrentSemantics.GetSymbolInfo(call).Symbol;
            if (!method.IsStatic)
                throw CompilationException.ToDatapackMethodCallsMustBeStatic;

            MCFunctionName methodName = nameManager.GetMethodName(CurrentSemantics, call, this);
            // Handle all compiler-known custom names.
            // This may also need a better system if there's more than one.
            if (methodName == "RunRaw") {
                HandleRunRaw(call);
                return;
            }

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

        private void HandleRunRaw(InvocationExpressionSyntax call) {
            var argument = call.ArgumentList.Arguments[0];
            if (argument.Expression is not LiteralExpressionSyntax lit
                || lit.Kind() != SyntaxKind.StringLiteralExpression) {
                AddCustomDiagnostic(DiagnosticRules.ToDatapackRunRawArgMustBeLiteral, call.GetLocation());
                return;
            }
            // `lit.Token.Text` returns the *full* string, as written in the IDE.
            // This includes the surrounding [@$]"", escaped chars, etc.
            // We need the actual value meant.
            // I currently don't allow other (@$) strings, so just assume "".
            string code = lit.Token.Text;
            code = code[1..(code.Length - 1)];
            code = System.Text.RegularExpressions.Regex.Unescape(code);
            if (code.StartsWith('/'))
                code = code[1..];
            AddCode(code);
        }

        private void HandleMCTestMethod(MethodDeclarationSyntax node, AttributeData attrib) {
            // Check correctness
            bool hasStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);
            bool hasNoArguments = node.ArityOfArguments() == 0;
            bool returnsInt = node.ReturnType.ChildTokensContain(SyntaxKind.IntKeyword);
            if (!hasStatic || !hasNoArguments || !returnsInt) {
                AddCustomDiagnostic(DiagnosticRules.MCTestAttributeIncorrect, node.GetLocation(), node.Identifier.Text);
                return;
            }

            int expected = (int)attrib.ConstructorArguments[0].Value;
            string fullyQualifiedName = NameManager.GetFullyQualifiedMethodName(CurrentSemantics, node);
            // As at this point, the actual method is done, it should have
            // assigned to #RET already. We can just freely read that here.
            AddCode($"execute if score #RET _ matches {expected} unless score #FAILSONLY _ matches 1 run tellraw @a [{{\"text\":\"Test \",\"color\":\"green\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_green\"}},{{\"text\":\" passed.\",\"color\":\"green\"}}]");
            AddCode($"execute if score #RET _ matches {expected} run scoreboard players add #TESTSUCCESSES _ 1");
            AddCode($"execute unless score #RET _ matches {expected} run tellraw @a [{{\"text\":\"Test \",\"color\":\"red\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_red\"}},{{\"text\":\" failed.\\n  Expected \",\"color\":\"red\"}},{{\"text\":\"{expected}\",\"bold\":true,\"color\":\"dark_red\"}},{{\"text\":\", but got \",\"color\":\"red\"}},{{\"score\":{{\"name\":\"#RET\",\"objective\":\"_\"}},\"bold\":true,\"color\":\"dark_red\"}},{{\"text\":\" instead.\",\"color\":\"red\"}}]");
            AddCode($"execute unless score #RET _ matches {expected} run scoreboard players add #TESTFAILURES _ 1");

            testFunctions.Add(nameManager.GetMethodName(CurrentSemantics, node, this));
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

        private MCFunctionName GetGotoFunctionName(int got) {
            if (gotoFunctionNames.TryGetValue(got, out MCFunctionName function))
                return function;
            function = GetBranchPath($"goto-label-{got}");
            gotoFunctionNames.Add(got, function);
            return function;
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
