using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all rewriters needed to turns all
    /// <tt>T1 operator +(T2 a, T3 b)</tt>'s referenced <tt>a + b</tt> into
    /// actual proper named methods that are called.
    /// </summary>
    // Roslyn recognises non-primitive things like `int3 == int3` here,
    // but primitive `int == int` return a `null` op.OperatorMethod.
    // This despite `int`'s TypeSymbol seeing the `op_Equality` and
    // `op_Inequality` in Type > Non-Public Members > MemberNames.
    // As such, there's some annoying manual handling, which I put here.
    public class NameOperatorsCategory : AbstractCategory<
        CopyOperatorsToNamedRewriter,
        RegisterOperatorsWalker,
        OperatorsToMethodCallsRewriter,
        RemoveOperatorRewriter
        > {

        static readonly Dictionary<string, string> supportedConversions = new() {
            { "+", "OPERATOR-ADD" },
            { "-", "OPERATOR-SUB" },
            { "*", "OPERATOR-MUL" },
            { "/", "OPERATOR-DIV" },
            { "%", "OPERATOR-MOD" },
            { "&", "OPERATOR-AND" },
            { "|", "OPERATOR-OR" },
            { "^", "OPERATOR-XOR" },
            { "!", "OPERATOR-NOT" },
            { "~", "OPERATOR-CPL"},
            { "<<", "OPERATOR-SL" },
            { ">>", "OPERATOR-SRA" },
            { ">>>", "OPERATOR-SRL" },
            { "==", "OPERATOR-EQ" },
            { "!=", "OPERATOR-NEQ" },
            { "<", "OPERATOR-LT" },
            { ">", "OPERATOR-GT" },
            { "<=", "OPERATOR-LEQ" },
            { ">=", "OPERATOR-GEQ" },
        };

        /// <summary>
        /// Returns the name the method of the operator should have.
        /// </summary>
        public static string GetMethodName(OperatorDeclarationSyntax op)
            => GetMethodName(op.OperatorToken.Text);
        /// <inheritdoc cref="GetMethodName(OperatorDeclarationSyntax)"/>
        public static string GetMethodName(string op) => supportedConversions[op];

        /// <summary>
        /// In a node <c>lhs OP rhs</c>, returns the fully qualified name to
        /// the OPERATOR-OP method name in two parts.
        /// </summary>
        public static (string containingType, string name) ParseOperator(SemanticModel model, BinaryExpressionSyntax node) {
            var op = (IBinaryOperation)model.GetOperation(node);
            var methodName = GetMethodName(node.OperatorToken.Text);

            if (op.OperatorMethod != null) {
                var containingType = op.OperatorMethod.ContainingType.ToDisplayString();
                return (containingType, methodName);
            } else {
                // Primitive or nonexistent.
                if (!model.TypesMatch(op.LeftOperand.Type, op.RightOperand.Type))
                    throw new System.NotImplementedException("TODO: Cast case");

                string fullyQualified;
                // haha this also needs something better lol
                if (model.TypesMatch(op.LeftOperand.Type, MCMirrorTypes.Int))
                    fullyQualified = MCMirrorTypes.IntFullyQualified;
                else if (model.TypesMatch(op.LeftOperand.Type, MCMirrorTypes.Bool))
                    fullyQualified = MCMirrorTypes.BoolFullyQualified;
                else if (model.TypesMatch(op.LeftOperand.Type, MCMirrorTypes.Float))
                    fullyQualified = MCMirrorTypes.FloatFullyQualified;
                else
                    throw CompilationException.OperatorsRequireUnderlyingMethod;

                // Guaranteed primitive.
                return (fullyQualified, methodName);
            }
        }

        /// <summary>
        /// In a node <c>OP rhs</c>, returns the fully qualified name to
        /// the OPERATOR-OP method name in two parts.
        /// </summary>
        public static (string containingType, string name) ParseOperator(SemanticModel model, PrefixUnaryExpressionSyntax node) {
            var op = (IUnaryOperation)model.GetOperation(node);
            var methodName = GetMethodName(node.OperatorToken.Text);

            if (op.OperatorMethod != null) {
                var containingType = op.OperatorMethod.ContainingType.ToDisplayString();
                return (containingType, methodName);
            } else {
                // Primitive or nonexistent.
                string fullyQualified;
                if (model.TypesMatch(op.Operand.Type, MCMirrorTypes.Int))
                    fullyQualified = MCMirrorTypes.IntFullyQualified;
                else if (model.TypesMatch(op.Operand.Type, MCMirrorTypes.Bool))
                    fullyQualified = MCMirrorTypes.BoolFullyQualified;
                else
                    throw CompilationException.OperatorsRequireUnderlyingMethod;

                // Guaranteed primitive.
                return (fullyQualified, methodName);
            }
        }

        /// <summary>
        /// In a node <c>lhs OP= rhs</c> (explicitely disallowing <c>lhs = rhs</c>
        /// simple assignment), returns the fully qualified name to the
        /// OPERATOR-OP method name in two parts.
        /// </summary>
        public static (string containingType, string name) ParseOperator(SemanticModel model, AssignmentExpressionSyntax node) {
            // TODO: ISimple is excluded by method assumptions. This does ICompound. But ICoalesce and IDeconstruction also exist.
            var op = (ICompoundAssignmentOperation)model.GetOperation(node);
            var methodName = GetMethodName(node.OperatorToken.Text[0..^1]);

            if (op.OperatorMethod != null) {
                var containingType = op.OperatorMethod.ContainingType.ToDisplayString();
                return (containingType, methodName);
            } else {
                // Primitive or nonexistent
                if (!model.TypesMatch(op.Target.Type, op.Value.Type))
                    throw new System.NotImplementedException("TODO: Cast case");

                string fullyQualified;
                if (model.TypesMatch(op.Target.Type, MCMirrorTypes.Int))
                    fullyQualified = MCMirrorTypes.IntFullyQualified;
                else if (model.TypesMatch(op.Target.Type, MCMirrorTypes.Bool))
                    fullyQualified = MCMirrorTypes.BoolFullyQualified;
                else
                    throw CompilationException.OperatorsRequireUnderlyingMethod;

                // Guaranteed primitive
                return (fullyQualified, methodName);
            }
        }
    }

    public class RegisterOperatorsWalker : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "OPERATOR-" };
    }

    /// <summary> Removes any method that is an operator declaration. </summary>
    public class RemoveOperatorRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node) => null;
    }
}
