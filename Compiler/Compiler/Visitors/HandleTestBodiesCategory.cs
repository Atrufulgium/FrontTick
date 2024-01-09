using Atrufulgium.FrontTick.Compiler.Datapack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// what'd'y'mean "put these in different files"
// that would make sense
namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This turns any code of the form
    /// <code>
    ///     [MCTest(expected)]
    ///     public Type Method() {
    ///         // code
    ///     }
    /// </code>
    /// into
    /// <code>
    ///     [MCTest(expected)]
    ///     public void Method() {
    ///         MCMirror.Internal.RawMCFunction.Run("scoreboard players set #RET _ -2122222222");
    ///         MCMirror.Internal.TestVariables.TestsSkipped += 1;
    ///         Type #expected = expected;
    ///         Type #actual = Method-TestBody();
    ///         if (#expected == #actual) {
    ///             MCMirror.Internal.TestVariables.TestsSucceeded += 1;
    ///             if (MCMirror.Internal.TestVariables.OnlyPrintFails == 0) {
    ///                 MCMirror.Internal.RawMCFunction.Run(/*positive result info*/);
    ///             }
    ///         } else {
    ///             MCMirror.Internal.TestVariables.TestsFailed += 1;
    ///             MCMirror.Internal.RawMCFunction.Run(/*negative result info*/);
    ///             MCMirror.Internal.CompileTime.Print(#expected);
    ///             MCMirror.Internal.CompileTime.Print(#actual);
    ///             MCMirror.Internal.RawMCFunction.Run("tellraw @a [\"\"]");
    ///         }
    ///         MCMirror.Internal.TestVariables.TestsSkipped -= 1;
    ///     }
    ///     
    ///     public Type TESTBODY-Method() {
    ///         // code
    ///     }
    /// </code>
    /// </para>
    /// </summary>
    public class HandleTestBodiesCategory : AbstractCategory<
        CopyTestBodiesRewriter,
        RegisterCopiedTestBodiesWalker,
        MainTestBodiesRewriter,
        CompleteTestrunnerRewriter
        > { }

    /// <summary>
    /// <para>
    /// Copies the body of
    /// <code>
    ///     [MCTest(expected)]
    ///     public Type Method() {
    ///         // code
    ///     }
    /// </code>
    /// into a new method <c>public Type TESTMETHOD-Method()</c>.
    /// </para>
    /// </summary>
    public class CopyTestBodiesRewriter : AbstractFullRewriter {

        /// <summary>
        /// A map between the fully qualified original test method to the fully
        /// qualified copied method.
        /// </summary>
        public readonly Dictionary<string, string> testMethodsToBodyMap = new();
        readonly List<MemberDeclarationSyntax> introducedMethods = new();

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitClassDeclarationRespectingNoCompile);

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitStructDeclarationRespectingNoCompile);

        SyntaxNode VisitTypeDeclarationSyntax<T>(T node, Func<T, SyntaxNode> basecall) where T : TypeDeclarationSyntax {
            introducedMethods.Clear();
            node = (T)basecall(node);
            return node.WithAdditionalMembers<T>(introducedMethods);
        }

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            // Don't dive deeper on non-test methods.
            if (!CurrentSemantics.TryGetAttributeOfType(node, MCMirrorTypes.MCTestAttribute, out _))
                return node;

            // Check correctness
            bool hasStatic = node.Modifiers.Any(SyntaxKind.StaticKeyword);
            bool hasNoArguments = node.ArityOfArguments() == 0;
            if (!hasStatic || !hasNoArguments) {
                AddCustomDiagnostic(DiagnosticRules.MCTestAttributeIncorrect, node.GetLocation(), node.Identifier.Text);
                return node;
            }

            var retType = node.ReturnType;
            var name = node.Identifier.Text;

            string fullyQualifiedThisName = NameManager.GetFullyQualifiedMethodName(CurrentSemantics, node);
            string fullyQualifiedCopyName = fullyQualifiedThisName.Replace(node.Identifier.Text, "TESTBODY-" + node.Identifier.Text);
            testMethodsToBodyMap[fullyQualifiedThisName] = fullyQualifiedCopyName;

            // Can actually copy the method.
            introducedMethods.Add(
                MethodDeclaration(retType, "TESTBODY-" + name)
                .WithBody(node.Body)
                .WithExpressionBody(node.ExpressionBody)
                .WithAddedModifier(SyntaxKind.StaticKeyword)
            );
            return node;
        }
    }

    public class RegisterCopiedTestBodiesWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "TESTBODY-" };
    }

    /// <summary>
    /// <para>
    /// Actually modifies the main test methods to call the body methods.
    /// </para>
    /// <para>
    /// It also adds the test methods to the main function tag and all more
    /// specific function tags.
    /// </para>
    /// </summary>
    public class MainTestBodiesRewriter : AbstractFullRewriter<CopyTestBodiesRewriter> {
        CopyTestBodiesRewriter CopyTestBodiesRewriter => Dependency1;

        /// <summary>
        /// This contains all function tags that collect a single class of
        /// tests.
        /// </summary>
        public ReadOnlyCollection<FunctionTag> PartialTestTags { get; private set; }
        readonly List<FunctionTag> partialTestTags = new();
        FunctionTag currentTag;
        /// <summary>
        /// This contains the function tag that collects all tests.
        /// </summary>
        public FunctionTag TestTag { get; private set; }

        public override void GlobalPreProcess() {
            PartialTestTags = new(partialTestTags);
            TestTag = new(nameManager.manespace, "test.json", sorted: false);
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitClassDeclarationRespectingNoCompile);

        public override SyntaxNode VisitStructDeclarationRespectingNoCompile(StructDeclarationSyntax node)
            => VisitTypeDeclarationSyntax(node, base.VisitStructDeclarationRespectingNoCompile);

        SyntaxNode VisitTypeDeclarationSyntax<T>(T node, Func<T, SyntaxNode> basecall) where T : TypeDeclarationSyntax {
            var namedSymbol = CurrentSemantics.GetDeclaredSymbol(node);
            var name = NameManager.NormalizeFunctionName(namedSymbol.Name);
            currentTag = new(nameManager.manespace, $"test-{name}.json", sorted: false);

            node = (T)basecall(node);
            if (currentTag.TaggedFunctions.Count > 0) {
                partialTestTags.Add(currentTag);
            }
            return node;
        }

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            // Don't dive deeper on non-test methods.
            if (!CurrentSemantics.TryGetAttributeOfType(node, MCMirrorTypes.MCTestAttribute, out var attrib))
                return node;
            currentTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, node, this));
            TestTag.AddToTag(nameManager.GetMethodName(CurrentSemantics, node, this));

            var retType = node.ReturnType;
            var expected = attrib.ArgumentList.Arguments[0].Expression;

            var pos = node.GetLocation().GetLineSpan();
            string path = System.IO.Path.GetFileName(pos.Path);
            string fullyQualifiedName = NameManager.GetFullyQualifiedMethodName(CurrentSemantics, node);
            string hover = $"\"hoverEvent\":{{\"action\":\"show_text\",\"contents\":[{{\"text\":\"File \",\"color\":\"gray\"}},{{\"text\":\"{path}\",\"color\":\"white\"}},{{\"text\":\"\\nLine \",\"color\":\"gray\"}},{{\"text\":\"{pos.StartLinePosition.Line}\",\"color\":\"white\"}},{{\"text\":\" Col \",\"color\":\"gray\"}},{{\"text\":\"{pos.StartLinePosition.Character}\",\"color\":\"white\"}}]}}";
            string successCommand = $"tellraw @a [{{\"text\":\"Test \",\"color\":\"green\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_green\",{hover}}},{{\"text\":\" passed.\",\"color\":\"green\"}}]";
            var failureCommand = $"tellraw @a [{{\"text\":\"Test \",\"color\":\"red\"}},{{\"text\":\"{fullyQualifiedName}\",\"color\":\"dark_red\",{hover}}},{{\"text\":\" failed.\",\"color\":\"red\"}}]";

            return node.WithReturnType(PredefinedType(Token(SyntaxKind.VoidKeyword)))
                .WithExpressionBody(null)
                .WithBody(
                Block(
                    // MCMirror.Internal.RawMCFunction.Run("scoreboard players set #RET _ -2122222222");
                    InvocationStatement(
                        MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                        StringLiteralExpression("scoreboard players set #RET _ -2122222222")
                    ),
                    // MCMirror.Internal.TestVariables.TestsSkipped += 1
                    AddAssignmentStatement(
                        MemberAccessExpression(MCMirrorTypes.Testrunner_TestsSkipped),
                        NumericLiteralExpression(1)
                    ),
                    // MyType #expected = expected
                    LocalDeclarationStatement(retType, "#expected", expected),
                    // MyType #actual = TESTBODY-MyMethod()
                    LocalDeclarationStatement(
                        retType, "#actual",
                        InvocationExpression(
                            MemberAccessExpression(
                                CopyTestBodiesRewriter.testMethodsToBodyMap[fullyQualifiedName]
                            )
                        )
                    ),
                    // if (#expected == #actual)
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            IdentifierName("#expected"),
                            IdentifierName("#actual")
                        ),
                        Block(
                            // MCMirror.Internal.TestVariables.TestsSucceeded += 1
                            AddAssignmentStatement(
                                MemberAccessExpression(MCMirrorTypes.Testrunner_TestsSucceeded),
                                NumericLiteralExpression(1)
                            ),
                            // if (MCMirror.Internal.TestVariables.OnlyPrintFails == 0)
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    MemberAccessExpression(MCMirrorTypes.Testrunner_OnlyPrintFails),
                                    NumericLiteralExpression(0)
                                ),
                                Block(
                                    // success handling
                                    InvocationStatement(
                                        MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                                        StringLiteralExpression(successCommand)
                                    )
                                )
                            )
                        ),
                        // else
                        ElseClause(
                            // MCMirror.Internal.TestVariables.TestsFailed += 1
                            AddAssignmentStatement(
                                MemberAccessExpression(MCMirrorTypes.Testrunner_TestsFailed),
                                NumericLiteralExpression(1)
                            ),
                            // Show failure header
                            InvocationStatement(
                                MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                                StringLiteralExpression(failureCommand)
                            ),
                            // MCMirror.Internal.CompileTime.Print(#expected)
                            InvocationStatement(
                                MemberAccessExpression(MCMirrorTypes.CompileTime_Print),
                                IdentifierName("#expected")
                            ),
                            // MCMirror.Internal.CompileTime.Print(#actual)
                            InvocationStatement(
                                MemberAccessExpression(MCMirrorTypes.CompileTime_Print),
                                IdentifierName("#actual")
                            ),
                            // MCMirror.Internal.CompileTime.RunRaw("tellraw @a [\"\"]")
                            InvocationStatement(
                                MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                                StringLiteralExpression("tellraw @a [\"\"]")
                            )
                        )
                    ),
                    // MCMirror.Internal.TestVariables.TestsSkipped -= 1
                    SubtractAssignmentStatement(
                        MemberAccessExpression(MCMirrorTypes.Testrunner_TestsSkipped),
                        NumericLiteralExpression(1)
                    )
                )
            );
        }
    }

    /// <summary>
    /// Fills up the `TestrunnerMenu()` method in `MCMirror.Internal.Testrunner`.
    /// At the same time, also adds the final methods to the test runner
    /// function tag and puts them into the datapack.
    /// </summary>
    public class CompleteTestrunnerRewriter : AbstractFullRewriter<MainTestBodiesRewriter> {
        MainTestBodiesRewriter MainTestBodiesRewriter => Dependency1;

        public override void GlobalPostProcess() {
            compiler.ManuallyFinalizeDatapackFile(MainTestBodiesRewriter.TestTag);
            foreach (var functionTag in MainTestBodiesRewriter.PartialTestTags)
                compiler.ManuallyFinalizeDatapackFile(functionTag);
        }

        public override SyntaxNode VisitClassDeclarationRespectingNoCompile(ClassDeclarationSyntax node) {
            if (!CurrentSemantics.TypesMatch(node, MCMirrorTypes.Testrunner))
                return node;
            // No need to do anything if there's no tests.
            if (MainTestBodiesRewriter.TestTag.TaggedFunctions.Count == 0)
                return node;
            return base.VisitClassDeclarationRespectingNoCompile(node);
        }

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            string id = node.Identifier.Text;
            if (id == MCMirrorTypes.Testrunner_PreprocessTestrunner_Unqualified) {
                var methodName = nameManager.GetMethodName(CurrentSemantics, node, this);
                MainTestBodiesRewriter.TestTag.PrependToTag(methodName);
                foreach (var tag in MainTestBodiesRewriter.PartialTestTags)
                    tag.PrependToTag(methodName);

            } else if (id == MCMirrorTypes.Testrunner_PostprocessTestrunner_Unqualified) {
                // Only difference with the above is Add instead of Prepend.
                var methodName = nameManager.GetMethodName(CurrentSemantics, node, this);
                MainTestBodiesRewriter.TestTag.AddToTag(methodName);
                foreach (var tag in MainTestBodiesRewriter.PartialTestTags)
                    tag.AddToTag(methodName);

            } else if (id == MCMirrorTypes.Testrunner_TestrunnerMenu_Unqualified) {
                // We want to each test category as a separate tellraw line.
                // After that, the "run all" button.
                string name, hoverCommand, command;
                int testCount;
                List<StatementSyntax> statementsToAdd = new();
                statementsToAdd.Add(InvocationStatement(
                        MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                        StringLiteralExpression("tellraw @p {\"text\":\"Available tests:\",\"color\":\"gray\"}")
                    )
                );
                foreach (var functionTag in MainTestBodiesRewriter.PartialTestTags) {
                    name = functionTag.Namespace + ":" + functionTag.Subpath.Replace(".json", "");
                    // Note that minecraft requires a `/` in hover commands.
                    hoverCommand = $"/function {nameManager.GetFunctionTagName(functionTag)}";
                    testCount = functionTag.TaggedFunctions.Count;
                    command = $"tellraw @a [{{\"text\":\"  Category §e{name}\",\"color\":\"white\",\"clickEvent\":{{\"action\":\"run_command\",\"value\":\"{hoverCommand}\"}},\"hoverEvent\":{{\"action\":\"show_text\",\"contents\":[\"Click to run tests\"]}}}},{{\"text\":\" ({testCount} tests)\",\"color\":\"gray\"}}]";
                    statementsToAdd.Add(
                        InvocationStatement(
                            MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                            StringLiteralExpression(command)
                        )
                    );
                }
                var allTag = MainTestBodiesRewriter.TestTag;
                hoverCommand = $"/function {nameManager.GetFunctionTagName(allTag)}";
                testCount = allTag.TaggedFunctions.Count;
                command = $"tellraw @a [{{\"text\":\"  All of the above\",\"color\":\"gold\",\"clickEvent\":{{\"action\":\"run_command\",\"value\":\"{hoverCommand}\"}},\"hoverEvent\":{{\"action\":\"show_text\",\"contents\":[\"Click to run tests\"]}}}},{{\"text\":\" ({testCount} tests)\",\"color\":\"gray\"}}]";
                statementsToAdd.Add(
                        InvocationStatement(
                            MemberAccessExpression(MCMirrorTypes.RawMCFunction_Run),
                            StringLiteralExpression(command)
                        )
                    );

                node = node.WithBody(
                    node.Body.WithPrependedStatement(statementsToAdd)
                );
            }
            return node;
        }
    }
}
