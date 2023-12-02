using System;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// The difference between <see cref="CompilationException"/>'s exceptions
    /// and <see cref="DiagnosticRules"/>' rules is that the former is not
    /// supposed to be visible to the end-user, but the latter is.
    /// </para>
    /// <para>
    /// Use the former if the code is supposed to be formatted by previous
    /// stages but may have failed somehow due to errors in the compiler, and
    /// the latter for errors in the end-user's code and not the compiler.
    /// </para>
    /// </summary>
    public class CompilationException : Exception {
        public CompilationException(string message) : base(message) { }

        public static CompilationException LoopsToGotoForInitNoDeclarationsAllowed
            => new("[Loops To Goto] A for loop may not contain a declaration in its initializer.");
        public static CompilationException LoopsToGotoOnlyWhileInWhileProcessing
            => new("[Loops to Goto] When processing while -> goto, there may not be any for, foreach, or do-while loops.");
        public static CompilationException OperatorsRequireUnderlyingMethod
            => new("[Ops to Methods] Every unary and binary operator requires an underlying method.");
        public static CompilationException ToDatapackAssignmentOpsMustBeSimpleOrArithmetic
            => new("[To Datapack] Assignments must be one of \"=\", \"+=\", \"-=\", \"*=\", \"/=\", or \"%=\".");
        public static CompilationException ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls
            => new("[To Datapack] \"a ∘= RHS\"'s RHS must be a literal, identifier, or method call. This includes \"assigning\" to returns in `return ...`.");
        public static CompilationException ToDatapackBranchesMustBeBlocks
            => new("[To Datapack] The if- and else-branch of a conditional must be a block ({}) and not a single statement.");
        public static CompilationException ToDatapackGotoLabelMustBeBlock
            => new("[To Datapack] A labeled statement may only label a block ({}) and nothing else.");
        public static CompilationException ToDatapackGotoMustBeLastBlockStatement
            => new("[To Datapack] In every block, goto must be the last statement -- nothing may follow, not even labels.");
        public static CompilationException ToDatapackIfConditionalMustBeIdentifierOrNegatedIdentifier
            => new("[To Datapack] The conditional of a if-statement must be of the form `identifier` or `!identifier` for some boolean variable `identifier`.");
        public static CompilationException ToDatapackDeclarationsMayNotBeInitializers
            => new("[To Datapack] Declarations may not be initializers.");
        public static CompilationException ToDatapackLiteralsIntegerOnly
            => new("[To Datapack] Literals may only be integers or `true`/`false` at this stage.");
        public static CompilationException ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals
            => new("[To Datapack] Calls' arguments must consist of identifiers or literals.");
        public static CompilationException ToDatapackMethodCallsMustBeStatic
            => new("[To Datapack] Calls may only target static methods.");
        public static CompilationException ToDatapackReturnDoesntMatchVoidness
            => new("[To Datapack] Either this method returns void but has a return of the form `return ...`, or this method doesn't return void but has a return of the form `return;`.");
        public static CompilationException ToDatapackReturnNoNonReturnAfterReturn
            => new("[To Datapack] (Conditional) returns must be the final statements in a method. No functionality allowed after that.");
        public static CompilationException ToDatapackStructsMustEventuallyInt
            => new("[To Datapack] Any used struct's members must eventually be of Int32 type.");
        public static CompilationException ToDatapackUnsupportedUnary
            => new("[To Datapack] The only supported unary operations are \"+literal\" and \"-literal\".");
        public static CompilationException ToDatapackUnsupportedStatementType
            => new("[To Datapack] Encountered an unsupported statement type while handling a statement.");
        public static CompilationException ToDatapackVariableNamesAreFromIdentifiersOrAccesses
            => new("[To Datapack] The only allowed variables to process to names are identifiers or accesses with `.`.");
        public static CompilationException ToDatapackVariablesFieldLocalOrParams
            => new("[To Datapack] The only supported variable types are class fields, method locals, or method parameters.");
    }
}
