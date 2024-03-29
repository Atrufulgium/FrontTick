﻿using Atrufulgium.FrontTick.Compiler.Datapack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            // Don't do methods that aren't meant to be compiled.
            if (CurrentSemantics.TryGetSemanticAttributeOfType(node, MCMirrorTypes.CustomCompiledAttribute, out _))
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

            // Add some debug stats
            List<string> attributes = new();
            foreach (var attribute in node.AttributeLists.SelectMany(al => al.Attributes))
                attributes.Add($"#   [{attribute}]");
            if (attributes.Count > 0)
                AddCode($"\n# Method Attributes:\n{string.Join('\n', attributes)}");

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
            } else if (statement is ThrowStatementSyntax th
                && CurrentSemantics.TypesMatch(th.Expression, MCMirrorTypes.UnreachableCodeException)) {
                // Also defined to be a noop.
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
            // Note to future self: This one is actually needed and some phases
            // use this assumption.
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

            if (assign.Right is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.DefaultLiteralExpression)) {
                // Well-defined due to it being a reserved keyword and not
                // a contextual keyword. In any case, nothing goes through
                // nameManager to end up here.
                rhsName = "default";
            } else if (TryGetIntegerLiteral(assign.Right, out int literal)) {
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
            MCFunctionName targetIfName = GetBranchPath("if-branch");
            string conditionIdentifier;
            if (ifst.Else != null)
                throw new ArgumentException("At this point the tree should have no else clauses left.");

            if (ifst.Statement is not BlockSyntax block)
                throw CompilationException.ToDatapackBranchesMustBeBlocks;

            // The if-statement must be of the form
            //   if (identifier)
            // or
            //   if (!identifier)
            // where `identifier` is a bool.
            // See also SimplifyIfConditionRewriter.
            // However, for semantic reasons (see GotoFlagifyRewriter's
            // comment for mor info), we also have
            //   if (someInt == constant)
            // to deal with separately.

            bool flipped;
            if (ifst.Condition is IdentifierNameSyntax id) {                // "if (id)"
                // Regular `if (identifier)`, handle later.
                flipped = false;
            } else if (ifst.Condition is PrefixUnaryExpressionSyntax un     // "if (!id2)" 
                && un.IsKind(SyntaxKind.LogicalNotExpression)
                && un.Operand is IdentifierNameSyntax id2) {
                // Flipped `if (!identifier)`, handle later.
                flipped = true;
                id = id2;
            } else if (ifst.Condition is BinaryExpressionSyntax bin         // "if (id3 == lit)"
                && bin.Left is IdentifierNameSyntax id3
                && bin.OperatorToken.Text == "=="
                && TryGetIntegerLiteral(bin.Right, out int lit)
                && CurrentSemantics.TypesMatch(id3, MCMirrorTypes.Int)) {
                // The annoying special case introduced by GotoFlagifyRewriter, handle now.
                string edgecaseCall = HandleBlock(block, targetIfName);
                conditionIdentifier = nameManager.GetVariableName(CurrentSemantics, id3, this);
                if (edgecaseCall != "")
                    AddCode($"execute if score {conditionIdentifier} _ matches {lit} run {edgecaseCall}");
                return;
            } else { 
                throw CompilationException.ToDatapackIfConditionalMustBeIdentifierOrNegatedIdentifier;
            }
            if (!CurrentSemantics.TypesMatch(id, MCMirrorTypes.Bool)) {
                throw CompilationException.ToDatapackIfConditionalMustBeIdentifierOrNegatedIdentifier;
            }

            conditionIdentifier = nameManager.GetVariableName(CurrentSemantics, id, this);

            bool equalsVariant = !flipped;
            string ifBranchMCConditional = equalsVariant ? "if" : "unless";

            string call = HandleBlock(block, targetIfName);
            if (call != "")
                AddCode($"execute {ifBranchMCConditional} score {conditionIdentifier} _ matches 1 run {call}");
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
            if (methodName == "CompileTime/RunRaw") {
                HandleRunRaw(call);
                return;
            }
            // Note that a lot of these have been handled already by
            /// <see cref="CompiletimeClassRewriter"/>
            // At this point, any left are an error.
            if (methodName.name.StartsWith("CompileTime/"))
                throw new ArgumentException("Unhandled compile time function remaining!");

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
            string prefix = "";
            if (unary.Kind() == SyntaxKind.UnaryPlusExpression)
                prefix = "";
            else if (unary.Kind() == SyntaxKind.UnaryMinusExpression) {
                prefix = "-";
            } else {
                throw CompilationException.ToDatapackUnsupportedUnary;
            }
            if (unary.Operand is LiteralExpressionSyntax lit) {
                value = GetIntegerLiteral(lit, prefix);
                return true;
            }
            value = 0;
            return false;
        }
        // Only call this from TryGetIntegerLiteral
        private static int GetIntegerLiteral(LiteralExpressionSyntax lit, string prefix = "") {
            if (bool.TryParse(prefix + lit.Token.ValueText, out bool b))
                return b ? 1 : 0;
            if (int.TryParse(prefix + lit.Token.ValueText, out int res))
                return res;
            throw CompilationException.ToDatapackLiteralsIntegerOnly;
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
            // This code somewhat copied to
            /// <see cref="CompiletimeClassRewriter.GetPrintComplexRunrawArgs(ITypeSymbol, List{string}, string, int)"/>

            // Note that `default` has special handling.
            if (rhs == "default" && op != "=")
                throw new ArgumentException("Only =default is allowed, no other ops.");

            // Todo: this will probably be pretty expensive in the long run. Cache (type,fields[]) in some dict.
            // Also, don't do a = a assignments. Those are stupid.
            if (CurrentSemantics.TypesMatch(type, MCMirrorTypes.Int) || CurrentSemantics.TypesMatch(type, MCMirrorTypes.Bool)) {
                if (rhs == "default")
                    AddCode($"scoreboard players set {lhs} _ 0");
                else if (!(op == "=" && lhs == rhs))
                    AddCode($"scoreboard players operation {lhs} _ {op} {rhs} _");
            } else if (type.IsPrimitive() && !CurrentSemantics.GetFullyQualifiedNameIncludingPrimitives(type).StartsWith("System")) {
                // (If GetFullyQualifiedNameIncludingPrimitives finds it, it ought to be implemented properly
                //  already, in which case we do reduce to ints.)
                throw CompilationException.ToDatapackStructsMustEventuallyInt;
            } else {
                foreach (var m in type.GetNonstaticFields()) {
                    ITypeSymbol fieldType = m.Type;
                    string name = m.Name;
                    if (rhs == "default")
                        AddAssignment($"{lhs}#{name}", rhs, op, fieldType);
                    else if (!(op == "=" && lhs == rhs))
                        AddAssignment($"{lhs}#{name}", $"{rhs}#{name}", op, fieldType);
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
