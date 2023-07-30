using Atrufulgium.FrontTick.Compiler.Datapack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using MCMirror.Internal;

namespace Atrufulgium.FrontTick.Compiler.Visitors
{
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
    public class ProcessedToDatapackWalker : AbstractFullWalker<
        SetupCategory,
        PreProcessCategory,
        FlattenNestedCallsRewriter,
        ReturnRewriter,
        GotoFlagifyRewriter,
        LoadTickWalker
    > {
        // TODO: ProcessedToDatapackWalker optimisation opportunities:
        // * Replace `operation += const` with `add const` (or `remove const` if negative)
        // * Replace `operation -= const` with `remove const` (or `add const` if negative)
        // * MCFunction files that are just a simple `function ...` can be skipped
        // * MCFunction files that are empty can have their callsite removed
        // (The above two points are implemented for files within one c# method,
        //  but not full generality.)
        // * Multiple `goto`s to the same label generate different files currently

        // TODO: ProcessedToDatapackWalker todo list:
        // * Allow min as `operation <`, max as `operation >`
        // * Allow swap `><`
        // * Can extract all arithmetic processing to their own structs/classes like `MCInt` that use `Run(..)`

        private GotoFlagifyRewriter GotoFlagifyRewriter => Dependency5;

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
        readonly Stack<MCFunctionFile> wipFiles = new();
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
        /// All methods with a [MCTest(int)] attribute.
        /// </summary>
        public FunctionTag testFunctions;

        public override void GlobalPreProcess() {
            testFunctions = new(nameManager.manespace, "test.json", sorted: false);
        }
        public override void GlobalPostProcess() {
            testFunctions.AddToTag(nameManager.TestPostProcessName);
        }

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            // Don't do methods that aren't meant to be compiled.
            if (CurrentSemantics.TryGetSemanticAttributeOfType(node, typeof(MCMirror.Internal.CustomCompiledAttribute), out _))
                return;
            if (node.ChildTokensContain(SyntaxKind.ExternKeyword))
                return;

            currentNode = node;
            branchCounter = 0;
            gotoFunctionNames = new();
            // Don't do the base-call as we're manually walking everything from
            // here on out, as the code must abide a very specific structure.
            MCFunctionName path = nameManager.GetMethodName(CurrentSemantics, node, this);

            HandleBlock(node.Body, path, pop: false);

            // If this method is a test, we need to add some post processing to
            // the mcfunction.
            if (CurrentSemantics.TryGetSemanticAttributeOfType(node, typeof(MCTestAttribute), out var attrib))
                HandleMCTestMethod(node, attrib);

            compiler.finishedCompilation.Add(wipFiles.Pop());
        }

        /// <summary>
        /// <para>
        /// Parses a block statement, and returns a string to call its content.
        /// </para>
        /// <para>
        /// This string is either just a <tt>function ...</tt>, or the file's
        /// content if it's just one line of <tt>.mcfunction</tt>, or the empty
        /// string if the generated function is empty.
        /// </para>
        /// </summary>
        /// <param name="block"> The block to parse. </param>
        /// <param name="reason">
        /// The mcfunction name of this generated scope, usually of the form
        /// <tt>method's mcfunction-specific</tt>.
        /// should be.
        /// </param>
        /// <param name="pop">
        /// Whether the returned string is of the above format because we pop
        /// here, or whether the returned string is empty and <paramref name="reason"/>'s
        /// generated file is not popped yet for future parsing.
        /// </param>
        /// <param name="storeOneliners">
        /// (When <paramref name="pop"/> is true) whether generated functions
        /// that are one line long are still commited to the list of all
        /// functions.
        /// </param>
        // We can do this by all prior work guaranteeing a 1:1
        // block <=> mcfunction file correspondence
        private string HandleBlock(
            BlockSyntax block,
            MCFunctionName reason,
            bool pop = true,
            bool storeOneliners = false
        ) {
            wipFiles.Push(new(reason));
            int stackSize = wipFiles.Count;

            foreach (var statement in block.Statements) {
                // (Don't want to consider labels, so extract the statement.
                //  Labels may nest.)
                var checkStatement = statement;
                while (checkStatement is LabeledStatementSyntax labeled)
                    checkStatement = labeled.Statement;

                HandleStatement(statement);
            }

            PopWIPStackUntilSize(stackSize);

            if (!pop) {
                return "";
            }

            int remainingFileSize = wipFiles.Peek().code.Count;
            if (remainingFileSize == 0) {
                wipFiles.Pop();
                return "";
            } else if (remainingFileSize == 1) {
                string command = wipFiles.Peek().code[0];
                if (storeOneliners)
                    compiler.finishedCompilation.Add(wipFiles.Pop());
                else
                    wipFiles.Pop();
                return command;
            } else {
                compiler.finishedCompilation.Add(wipFiles.Pop());
                return $"function {reason}";
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
            // gotos already. The final `return` is in a labeled block we ignore.
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
                && assign.Left is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                string lhsName = nameManager.GetVariableName(CurrentSemantics, assign.Left, this);
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
            /// <see cref="nameManager.GetRetName()"/>
            // which isn't qualified by
            /// <see cref="nameManager.GetVariableName(SemanticModel, IdentifierNameSyntax, ICustomDiagnosable)"/>'s
            // special behaviour in that case.
            bool lhsIsRet = lhsName == nameManager.GetRetName();

            if (TryGetIntegerLiteral(assign.Right, out int literal)) {
                isSetCommand = op == "=";
                rhsName = literal.ToString();
                if (!isSetCommand)
                    rhsName = nameManager.GetConstName(literal);
            } else if (assign.Right is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                rhsName = nameManager.GetVariableName(CurrentSemantics, assign.Right, this);
            } else if (assign.Right is InvocationExpressionSyntax rhsCall) {
                HandleInvocation(rhsCall);
                rhsName = nameManager.GetRetName();
                // No need to do anything on
                //    #RET _ = RET _
                if (lhsIsRet && op == "=")
                    return;
            } else {
                throw CompilationException.ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls;
            }
            if (isSetCommand) // Automatic integer rhs, so is single integer!
                AddCode($"scoreboard players set {lhsName} _ {rhsName}");
            else
                AddAssignment(lhsName, rhsName, op, CurrentSemantics.GetTypeInfo(assign.Left).Type);
        }

        private void HandleIfElseGroup(IfStatementSyntax ifst) {
            MCFunctionName targetIfName = GetBranchPath("if-branch"),
                targetElseName = null;
            string conditionIdentifier;
            bool hasElse = ifst.Else != null;
            if (hasElse)
                targetElseName = GetBranchPath("else-branch");

            // The if-statement must be of the form
            //   if (identifier != literal) [or ==]
            // TODO: >, >=, <, <=

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

            if (ifst.Statement is not BlockSyntax block)
                throw CompilationException.ToDatapackBranchesMustBeBlocks;

            string call = HandleBlock(block, targetIfName);
            if (call != "")
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {call}");

            if (hasElse) {
                if (ifst.Else.Statement is not BlockSyntax elseBlock)
                    throw CompilationException.ToDatapackBranchesMustBeBlocks;

                call = HandleBlock(elseBlock, targetElseName);
                if (call != "")
                    AddCode($"execute {elseBranchMCConditional} score {conditionIdentifier} _ matches {rhsValue} run {call}");
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

            if (label.Statement is not BlockSyntax block)
                throw CompilationException.ToDatapackGotoLabelMustBeBlock;

            // Even if this place doesn't need it, labels exist for a reason.
            // As such, store the labeled part also even if the result is just
            // one line long.
            string call = HandleBlock(block, gotoBranch, storeOneliners: true);
            if (call != "")
                AddCode(call);
        }

        private void HandleInvocation(InvocationExpressionSyntax call) {
            // Note to future self: This may be null if the `call` is malformed.
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
            // "VarName" has been processed already by
            /// <see cref="VarNameMethodRewriter"/>

            // Note: Copied somewhat below.
            for (int i = 0; i < call.ArgumentList.Arguments.Count; i++) {
                var arg = call.ArgumentList.Arguments[i];
                // No need to copy in on an `out`.
                if (arg.ChildTokensContain(SyntaxKind.OutKeyword))
                    continue;

                string argName = nameManager.GetArgumentName(methodName, i);
                // Too copypastay of the statement case, this code's the sketch anyway
                if (TryGetIntegerLiteral(arg.Expression, out int literal)) {
                    // Again, integer rhs => lhs is of integer type
                    AddCode($"scoreboard players set {argName} _ {literal}");
                } else if (arg.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                    string rhsName = nameManager.GetVariableName(CurrentSemantics, arg.Expression, this);
                    AddAssignment(argName, rhsName, "=", CurrentSemantics.GetTypeInfo(arg.Expression).Type);
                } else {
                    throw CompilationException.ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals;
                }
            }

            AddCode($"function {methodName}");

            // Copy back variables on `out` and `ref`.
            // Copy of prior code with only the variable case.
            for (int i = 0; i < call.ArgumentList.Arguments.Count; i++) {
                var arg = call.ArgumentList.Arguments[i];
                // No need to copy out on `in` and nothing.
                if (!(arg.ChildTokensContain(SyntaxKind.OutKeyword)
                    || arg.ChildTokensContain(SyntaxKind.RefKeyword)))
                    continue;

                string argName = nameManager.GetArgumentName(methodName, i);
                // By C# guarantees, we are not an integer literal, so by our
                // own guarantees, we are an identifiername or member access.
                string rhsName = nameManager.GetVariableName(CurrentSemantics, arg.Expression, this);
                AddAssignment(rhsName, argName, "=", CurrentSemantics.GetTypeInfo(arg.Expression).Type);
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

            testFunctions.AddToTag(nameManager.GetMethodName(CurrentSemantics, node, this));
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
        /// Adds code to the currently considered datapack file for any
        /// assignment <tt>lhs op rhs</tt>. It takes into account the
        /// multiple values that need to be copied for larger structs.
        /// </para>
        /// </summary>
        private void AddAssignment(string lhs, string rhs, string op, ITypeSymbol type) {
            // Todo: this will probably be pretty expensive in the long run. Cache (type,fields[]) in some dict.
            if (CurrentSemantics.TypesMatch(type, typeof(int))) {
                AddCode($"scoreboard players operation {lhs} _ {op} {rhs} _");
            } else if (type.IsPrimitive()) {
                throw CompilationException.ToDatapackStructsMustEventuallyInt;
            } else {
                foreach (var m in type.GetMembers().OfType<IFieldSymbol>()) {
                    ITypeSymbol fieldType = m.Type;
                    string name = m.Name;
                    AddAssignment(nameManager.GetCombinedName(lhs, name), nameManager.GetCombinedName(rhs, name), op, fieldType);
                }
            }
        }

        /// <summary>
        /// Finishes the compilation of all work-in-progress datapack files
        /// until the remaining stack of them is of a certain size.
        /// </summary>
        private void PopWIPStackUntilSize(int size) {
            while (wipFiles.Count > size) {
                var finished = wipFiles.Pop();
                compiler.finishedCompilation.Add(finished);
            }
        }
    }
}
