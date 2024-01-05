using Atrufulgium.FrontTick.Compiler.Visitors;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Class that contains the names of various MCMirror types.
    /// While it's not yet used, the types used should be annotated with a
    /// <c>[CompilerUsesName]</c> attribute to tell the developer that renaming
    /// them might not be the best idea.
    /// </summary>
    // I hate that this is necessary. It's exceptionally jank.
    // However, this project can not depend on MCMirror. MCMirror uses its own
    // custom System implementation, and depending on it makes this project also
    // use it. I *could* separate MCMirror and the system implementation, but
    // that's just asking for headache bugs later down the line.
    // This is jank, but simple and it works.
    public static class MCMirrorTypes {
        // MCMirror.xxx
        public static readonly string MCFunctionAttribute = "MCMirror.MCFunctionAttribute";
        public static readonly string LoadAttribute = "MCMirror.LoadAttribute";
        public static readonly string NBTAttribute = "MCMirror.NBTAttribute";
        public static readonly string TickAttribute = "MCMirror.TickAttribute";
        public static readonly string TrueLoadAttribute = "MCMirror.TrueLoadAttribute";

        // MCMirror.Internal.xxx
        public static readonly string CompilerUsesNameAttribute = "MCMirror.Internal.CompilerUsesNameAttribute"; // not yet
        public static readonly string CompileTime_VarName = "MCMirror.Internal.CompileTime.VarName";
        public static readonly string CustomCompiledAttribute = "MCMirror.Internal.CustomCompiledAttribute";
        public static readonly string MCTestAttribute = "MCMirror.Internal.MCTestAttribute";
        public static readonly string NoCompileAttribute = "MCMirror.Internal.NoCompileAttribute";
        public static readonly string RawMCFunction_Run = "MCMirror.Internal.RawMCFunction.Run";
        public static readonly string Testrunner = "MCMirror.Internal.Testrunner";
        public static readonly string Testrunner_TestsSucceeded = "MCMirror.Internal.Testrunner.TestsSucceeded";
        public static readonly string Testrunner_TestsFailed = "MCMirror.Internal.Testrunner.TestsFailed";
        public static readonly string Testrunner_TestsSkipped = "MCMirror.Internal.Testrunner.TestsSkipped";
        public static readonly string Testrunner_OnlyPrintFails = "MCMirror.Internal.Testrunner.OnlyPrintFails";
        public static readonly string Testrunner_TestrunnerMenu_Unqualified = "TestrunnerMenu";
        public static readonly string Testrunner_PreprocessTestrunner_Unqualified = "PreprocessTestrunner";
        public static readonly string Testrunner_PostprocessTestrunner_Unqualified = "PostprocessTestrunner";
        public static readonly string TrueLoadManager = "MCMirror.Internal.TrueLoadManager";
        public static readonly string TrueLoadManager_TrueLoadTagname = "internal/--trueload--";
        public static readonly string UnreachableCodeException = "MCMirror.Internal.UnreachableCodeException";
        public static readonly string UnreachableCodeException_Exception = "MCMirror.Internal.UnreachableCodeException.Exception";

        // System.xxx:
        // These are (unfortunately) used all over the place as
        // (unfortunately) Roslyn handles primitives as a special case just
        // about everywhere, while to me, they're just any old type.
        // When adding a primitive, be sure to also update all following
        // locations. (Ctrl-clicky-click the method name.)
        /// <see cref="SemanticExtensionMethods.GetFullyQualifiedNameIncludingPrimitives(SemanticModel, ITypeSymbol)"/>
        /// <see cref="RewritePrimitiveLiteralsRewriter.VisitLiteralExpression(LiteralExpressionSyntax)"/>
        // Also, only tangentially related, but don't forget to update
        /// <see cref="Datapack.FullDatapack.skipInternalSpecifiers"/>

        public static readonly string Bool = "bool";
        public static readonly string BoolFullyQualified = "System.Boolean";
        public static readonly string Int = "int";
        public static readonly string IntFullyQualified = "System.Int32";
        public static readonly string Int_Equals_PostOperatorsToMethodCalls = "System.Int32." + NameOperatorsCategory.GetMethodName("==");
        public static readonly string Float = "float";
        public static readonly string FloatFullyQualified = "System.Single";
        public static readonly string Float_PositiveZero = "System.Single.PositiveZero";
        public static readonly string UInt = "uint";
        public static readonly string UIntFullyQualified = "System.UInt32";
        public static readonly string UInt_Zero = "System.UInt32.Zero";
    }
}
