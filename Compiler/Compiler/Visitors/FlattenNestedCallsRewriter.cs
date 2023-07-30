using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This rewriter turns code of the form
    /// <code>
    ///     a = Method1(Method2(a,Method3(b)),c,Method4(d));
    /// </code>
    /// into code of the form
    /// <code>
    ///     {
    ///         var temp0 = Method3(b);
    ///         var temp1 = Method2(a, temp0);
    ///         var temp2 = Method4(d);
    ///         a = Method1(temp1, c, temp2);
    ///     }
    /// </code>
    /// In other words, it ensures no call is contained in another.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is only partially implemented for conditions, but since those
    /// aren't yet extracted if complicated anyways, they're untested.
    /// So for if/while/for/etc this doesn't yet work.
    /// </remarks>
    // Very annoying note: expressions are *everywhere* and extraction cannot
    // be handled uniformly. E.g. within statements calls should be handled
    // differently from while conditions.
    // Calls themselves will add to "priorDeclarations", and statement
    // handlers should check whether anything got introduced to put in front.

    // Quick list of all possible things containing calls, from
    // https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Generated/CSharp.Generated.g4
    #region possibilities
    //   Implementing by just putting it before the relevant statement is safe:
    // checked_statement
    // expression_statement
    // if_statement
    // local_declaration_statement
    // lock_statement
    // return_statement
    // switch_statement
    // throw_statement
    // yield_statement
    //
    //   Implementing needs fancier stuff:
    // common_for_each_statement
    // do_statement
    // for_statement
    // labeled_statement
    // while_statement
    //
    //   (Probably) won't implement:
    // fixed_statement
    // using_statement
    #endregion
    // Anything not implemented will throw due to later assumptions, so there's
    // no chance of silently failing.
    public class FlattenNestedCallsRewriter : AbstractFullRewriter {

        int tempCounter = 0;
        readonly List<StatementSyntax> priorDeclarations = new();

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            // We need uniqueness of variables inside a method, and for consistency,
            // start all methods by counting from zero.
            tempCounter = 0;
            return base.VisitMethodDeclarationRespectingNoCompile(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
            // Go through the arguments of this call, and extract any arguments
            // that are calls themselves into a temporary variable.
            // Repeat this on both the extracted call, and this updated call.
            // Note we need to do all extractions in one pass if we don't want
            // to encounter the "Not in syntax tree'-semantics-error/having to
            // update the semantics a billion times.
            List<ArgumentSyntax> newArguments = new();

            foreach(var arg in node.ArgumentList.Arguments) {
                if (arg.Expression is not InvocationExpressionSyntax call) {
                    newArguments.Add(arg);
                    continue;
                }
                // We *are* a call argument and work is to be done.
                var retType = CurrentSemantics.GetTypeInfo(call).Type;

                // Recurse upward to guarantee there are no calling arguments
                // anymore. This prevents this part of the method from running
                // on a modified bit of tree which Roslyn doesn't like when you
                // combine it with semantics. We only need the rettype which we
                // grab above before any modifications.
                // That this also adds to priorDeclarations is fine.
                call = (InvocationExpressionSyntax)VisitInvocationExpression(call);

                var name = GetTempName();
                priorDeclarations.Add(
                    LocalDeclarationStatement(
                        retType,
                        name
                    )
                );
                priorDeclarations.Add(
                    AssignmentStatement(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(name),
                        call
                    )
                );
                newArguments.Add(Argument(IdentifierName(name)));
            }

            return node.WithArgumentList(ArgumentList(SeparatedList(newArguments)));
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            // Prevent nested blocks where they don't belong.
            return ((BlockSyntax)base.VisitBlock(node)).Flattened();
        }

        string GetTempName() => $"#CALLTEMP{tempCounter++}";
        
        // From here on out the list of possible call-containers.
        // First: the easy ones
        /// <summary>
        /// This just handles statements by putting any generated declarations
        /// in front of the given statement.
        /// </summary>
        SyntaxNode VisitEasyStatement<T>(T statement, Func<T, SyntaxNode> baseCall) {
            StatementSyntax handled = (StatementSyntax)baseCall(statement);
            if (priorDeclarations.Count == 0) {
                return handled;
            }
            List<StatementSyntax> introducedBlock = new(priorDeclarations) { handled };
            priorDeclarations.Clear();
            return VisitBlock(Block(introducedBlock));
        }

        // EUEUEUeueueeuughh
        public override SyntaxNode VisitCheckedStatement(CheckedStatementSyntax node) => VisitEasyStatement(node, base.VisitCheckedStatement);
        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node) => VisitEasyStatement(node, base.VisitExpressionStatement);
        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) => VisitEasyStatement(node, base.VisitIfStatement);
        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => VisitEasyStatement(node, base.VisitLocalDeclarationStatement);
        public override SyntaxNode VisitLockStatement(LockStatementSyntax node) => VisitEasyStatement(node, base.VisitLockStatement);
        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node) => VisitEasyStatement(node, base.VisitReturnStatement);
        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node) => VisitEasyStatement(node, base.VisitSwitchStatement);
        public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node) => VisitEasyStatement(node, base.VisitThrowStatement);
        public override SyntaxNode VisitYieldStatement(YieldStatementSyntax node) => VisitEasyStatement(node, base.VisitYieldStatement);

        // Second: the hard ones
        public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node) {
            // All introduced statements must be put after the label.
            StatementSyntax handled = (StatementSyntax)base.Visit(node.Statement);
            if (priorDeclarations.Count == 0) {
                return LabeledStatement(
                    node.Identifier.Text,
                    handled
                );
            }
            List<StatementSyntax> introducedBlock = new(priorDeclarations) { handled };
            priorDeclarations.Clear();
            return VisitLabeledStatement(
                LabeledStatement(
                    node.Identifier.Text,
                    Block(introducedBlock)
                )
            );
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node) {
            // A nested condition is really nasty. Rewrite it into
            // while(true) { /*introduced stuff*/ if(FinalMethod()) {break; } ..
            // Make sure to only parse the condition first. The main body may
            // also contain currently-irrelevant nested calls.
            ExpressionSyntax handledCondition = (ExpressionSyntax)base.Visit(node.Condition);
            if (priorDeclarations.Count == 0) {
                return handledCondition;
            }
            var breakCheck = IfStatement(handledCondition, BreakStatement());
            List<StatementSyntax> introducedBlock = new(priorDeclarations) { breakCheck };
            priorDeclarations.Clear();
            return VisitWhileStatement(
                WhileStatement(
                    LiteralExpression(SyntaxKind.TrueKeyword),
                    Block(node.Statement).WithPrependedStatement(introducedBlock)
                )
            );
        }

        // Note: These can all be collapsed into above's while if we first apply
        /// <see cref="LoopsToGotoCategory"/>'s
        // BlahToBlahRewriters. However, this would require generated code with
        // much stricter requirements, so both options suck tbh.
        public override SyntaxNode VisitForStatement(ForStatementSyntax node) {
            throw new NotImplementedException();
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node) {
            throw new NotImplementedException();
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node) {
            throw new NotImplementedException();
        }

        public override SyntaxNode VisitForEachVariableStatement(ForEachVariableStatementSyntax node) {
            throw new NotImplementedException();
        }
    }
}
