using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all rewriters needed to turns all
    /// <tt>T1 operator +(T2 a, T3 b)</tt>'s referenced <tt>a + b</tt> into
    /// actual proper named methods that are called.
    /// </summary>
    public class NameOperatorsCategory : AbstractFullWalker<
        CopyOperatorsToNamedRewriter,
        RegisterOperatorsCategory,
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
    }

    public class RegisterOperatorsCategory : AbstractRegisterMethodsByPrefixWalker {
        public override string[] CharacteristicString => new[] { "OPERATOR-" };
    }

    /// <summary> Removes any method that is an operator declaration. </summary>
    public class RemoveOperatorRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node) => null;
    }
}
