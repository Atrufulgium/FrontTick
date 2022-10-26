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
    public class ProcessedToDatapackWalker : AbstractFullWalker<SetupCategory, PreProcessCategory, ReturnRewriter, GotoFlagifyRewriter> {
        // TODO: ProcessedToDatapackWalker optimisation opportunities:
        // * Replace `operation += const` with `add const` (or `remove const` if negative)
        // * Replace `operation -= const` with `remove const` (or `add const` if negative)
        // * MCFunction files that are just a simple `function ...` can be skipped
        // * MCFunction files that are empty can have their callsite removed
        // * Multiple `goto`s to the same label generate different files currently (TODO: After the update, is this still true?)

        // TODO: ProcessedToDatapackWalker todo list:
        // * Allow min as `operation <`, max as `operation >`
        // * Allow swap `><`
        // * Can extract all arithmetic processing to their own structs/classes like `MCInt` that use `Run(..)`

        private GotoFlagifyRewriter GotoFlagifyRewriter => Dependency4;

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
        // but needs manual resetting in
        /// <see cref="VisitMethodDeclaration(MethodDeclarationSyntax)"/>
        int branchCounter;
        Dictionary<int, MCFunctionName> gotoFunctionNames;

        /// <summary>
        /// All methods with a [MCTest(int)] attribute. To be collected to be
        /// put into a minecraft function tag at a later stage.
        /// </summary>
        public List<MCFunctionName> testFunctions = new();

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            currentNode = node;
            branchCounter = 0;
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

            compiler.finishedCompilation.Add(wipFiles.Pop());
        }

        // TODO: blocks should be in 1:1 correspondence with mcfunction files at this point. Implement wipFiles.Push/Pop here, and only here.
        // Can also add a "reason"-or-something string argument for the scope increase name.
        private void HandleBlock(BlockSyntax block) {
            bool directlyAfterGoto = false;

            foreach (var statement in block.Statements) {
                // (Don't want to consider labels, so extract the statement.
                //  Labels may nest.)
                var checkStatement = statement;
                while (checkStatement is LabeledStatementSyntax labeled)
                    checkStatement = labeled.Statement;

                // Nothing may follow gotos -- not even labels.
                // (This is because splitting the method like that is basically
                //  a function call. Do *not* put it in the same method then.
                //  Banning goto from user-code is fine (apart from multiple
                //  break), and auto-generated code does not create the
                //  `goto ..; label: ..` construction, so this also just
                //  should not happen.)
                if (directlyAfterGoto)
                    throw CompilationException.ToDatapackGotoMustBeLastBlockStatement;

                HandleStatement(statement);
            }
        }

        private void HandleStatement(StatementSyntax statement) {
            if (statement is LabeledStatementSyntax labeled) {
                HandleGotoLabel(labeled);
            } else if (statement is IfStatementSyntax ifst) {
                HandleIfElseGroup(ifst);
            } else if (statement is LocalDeclarationStatementSyntax decl) {
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
            // blocks are supposed to correspond to mcfunctions at this point,
            // and having them nested for no reason is seriously wrong then.
            // Note II: No ReturnStatementSyntax. This has been processed into
            // gotos already.
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

            // The lhs can be created by
            /// <see cref="ReturnRewriter"/>
            // to be
            /// <see cref="NameManager.GetRetName()"/>
            // which isn't qualified by
            /// <see cref="NameManager.GetVariableName(SemanticModel, IdentifierNameSyntax, ICustomDiagnosable)"/>'s
            // special behaviour in that case.
            bool lhsIsRet = lhsName == NameManager.GetRetName();

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
                // No need to do anything on
                //    #RET _ = RET _
                if (lhsIsRet && op == "=")
                    return;
            } else {
                throw CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls;
            }
            if (isSetCommand)
                AddCode($"scoreboard players set {lhsName} _ {rhsName}");
            else
                AddCode($"scoreboard players operation {lhsName} _ {op} {rhsName} _");
        }

        // "private void HandleIfElseSoup"
        private void HandleIfElseGroup(IfStatementSyntax ifst) {
            MCFunctionName targetIfName = GetBranchPath("if-branch"),
                targetElseName = null;
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
            // TODO: Guarantee this in a seperate pass.
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

            wipFiles.Push(new(targetIfName));
            int targetIfStackSize = wipFiles.Count;
            if (ifst.Statement is BlockSyntax block)
                HandleBlock(block);
            else
                throw CompilationException.ToDatapackBranchesMustBeBlocks;
            // The above handling of blocks can introduce more wipFiles (via
            // gotos and such).
            // Every such file will be fully processed and done by the time we
            // return here. As such, we need to pop until we're back at the if-
            // block that brought us here in the first place.
            PopWIPStackUntilSize(targetIfStackSize);

            // Don't do the extra file if it's just one command.
            // (Naturally, don't do anything if it's nothing.)
            int branchSize = wipFiles.Peek().code.Count;
            if (branchSize == 1) {
                string command = wipFiles.Peek().code[0];
                wipFiles.Pop();
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {command}");
            } else if (branchSize > 1) {
                compiler.finishedCompilation.Add(wipFiles.Pop());
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run function {targetIfName}");
            } else {
                wipFiles.Pop();
            }

            if (hasElse) {
                // Same as above, but for else.
                // This stuff seems generalizable but if it remains these two
                // I'm not gonna bother.
                if (ifst.Else.Statement is not BlockSyntax elseBlock)
                    throw CompilationException.ToDatapackBranchesMustBeBlocks;

                wipFiles.Push(new(targetElseName));
                int targetElseStackSize = wipFiles.Count;
                HandleBlock(elseBlock);
                PopWIPStackUntilSize(targetElseStackSize);

                branchSize = wipFiles.Peek().code.Count;
                if (branchSize == 1) {
                    string command = wipFiles.Peek().code[0];
                    wipFiles.Pop();
                    AddCode($"execute {elseBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {command}");
                } else if (branchSize > 1) {
                    compiler.finishedCompilation.Add(wipFiles.Pop());
                    AddCode($"execute {elseBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run function {targetElseName}");
                } else {
                    wipFiles.Pop();
                }
            }
        }

        private void HandleGoto(GotoStatementSyntax got) {
            /// <see cref="GotoFlagifyRewriter"/>
            // ensures that the program is now in the correct form that
            // replacing a goto with a function call is fine.
            // Also, the "goto #ret-label" has been removed by it also,
            // so don't even need to consider ignoring it.
            string name = got.Target();
            int id = GotoFlagifyRewriter.GotoLabelToScoreboardID(name);
            var gotoLabel = GetGotoFunctionName(id);
            AddCode($"function {gotoLabel}");
        }

        private void HandleGotoLabel(LabeledStatementSyntax label) {
            string name = label.Identifier.Text;
            // If we're the label of the method's final return statement,
            // there is no need to do anything as the content is useless
            // at this point.
            if (name == NameManager.GetRetGotoName()) {
                return;
            }

            // Otherwise, there is a statement that needs processing.
            var gotoBranch = GetGotoFunctionName(GotoFlagifyRewriter.GotoLabelToScoreboardID(name));
            AddCode($"function {gotoBranch}");

            wipFiles.Push(new(gotoBranch));
            // We need all statements after to be in scope of this goto label.
            int afterGotoStackSize = wipFiles.Count;
            if (label.Statement is not BlockSyntax block)
                throw CompilationException.ToDatapackGotoLabelMustBeBlock;
            HandleBlock(block);
            PopWIPStackUntilSize(afterGotoStackSize);
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
                string argName = NameManager.GetArgumentName(methodName, i);
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
            // TODO: Proper string management.
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
            // To also check for not-completely run methods, we should keep
            // track fo a "skipped" variable, incremented at the start, and
            // decremented at the end.
            wipFiles.Peek().code.Insert(0, "scoreboard players add #TESTSSKIPPED _ 1");
            var pos = node.GetLocation().GetLineSpan();
            string path = System.IO.Path.GetFileName(pos.Path);
            string hover = $"\"hoverEvent\":{{\"action\":\"show_text\",\"contents\":[{{\"text\":\"File \",\"color\":\"gray\"}},{{\"text\":\"{path}\",\"color\":\"white\"}},{{\"text\":\"\\nLine \",\"color\":\"gray\"}},{{\"text\":\"{pos.StartLinePosition.Line}\",\"color\":\"white\"}},{{\"text\":\" Col \",\"color\":\"gray\"}},{{\"text\":\"{pos.StartLinePosition.Character}\",\"color\":\"white\"}}]}}";
            AddCode($"execute if score #RET _ matches {expected} unless score #FAILSONLY _ matches 1 run tellraw @a [{{\"text\":\"Test \",\"color\":\"green\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_green\",{hover}}},{{\"text\":\" passed.\",\"color\":\"green\"}}]");
            AddCode($"execute if score #RET _ matches {expected} run scoreboard players add #TESTSUCCESSES _ 1");
            AddCode($"execute unless score #RET _ matches {expected} run tellraw @a [{{\"text\":\"Test \",\"color\":\"red\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_red\",{hover}}},{{\"text\":\" failed.\\n  Expected \",\"color\":\"red\"}},{{\"text\":\"{expected}\",\"bold\":true,\"color\":\"dark_red\"}},{{\"text\":\", but got \",\"color\":\"red\"}},{{\"score\":{{\"name\":\"#RET\",\"objective\":\"_\"}},\"bold\":true,\"color\":\"dark_red\"}},{{\"text\":\" instead.\",\"color\":\"red\"}}]");
            AddCode($"execute unless score #RET _ matches {expected} run scoreboard players add #TESTFAILURES _ 1");
            AddCode("scoreboard players remove #TESTSSKIPPED _ 1");

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

        /// <summary>
        /// <para>
        /// Finishes the compilation of all work-in-progress datapack files
        /// until the remaining stack of them is of a certain size.
        /// </para>
        /// <para>
        /// This returns the number of lines in the final popped file, i.e. the
        /// file at index size+1.
        /// </para>
        /// </summary>
        private int PopWIPStackUntilSize(int size) {
            int lastCodeSize = -1;
            while (wipFiles.Count > size) {
                var finished = wipFiles.Pop();
                lastCodeSize = finished.code.Count;
                compiler.finishedCompilation.Add(finished);
            }

            return lastCodeSize;
        }
    }
}
