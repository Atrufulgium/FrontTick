using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.FullWalkers {
    /// <summary>
    /// A walker for turning the tree, fully processed into suitable form,
    /// into the actual datapack.
    /// </summary>
    public class ProcessedToDatapackWalker : AbstractFullWalker {

        public ProcessedToDatapackWalker(Compiler compiler) : base(compiler) { }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            string path = CurrentEntryPoint.mcFunctionName;
            DatapackFile finishedFile = new DatapackFile(path, compiler.manespace);


            // The important stuff


            compiler.finishedCompilation.Add(finishedFile);
        }
    }
}
