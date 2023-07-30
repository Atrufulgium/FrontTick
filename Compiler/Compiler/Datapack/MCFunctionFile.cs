using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Datapack {
    /// <summary>
    /// Contains a compiled file ready for datapack insertion.
    /// </summary>
    public class MCFunctionFile : IDatapackFile {
        /// <summary>
        /// The relative location of this file, excluding the
        /// <c>.mcfunction</c> suffix and including namespace prefix.
        /// </summary>
        public MCFunctionName Path { get; private set; }

        /// <summary>
        /// All code, separated line by line for easy additions.
        /// </summary>
        public List<string> code = new();

        /// <param name="path">
        /// The relative location of this file, excluding the
        /// <c>.mcfunction</c> suffix. This should already be normalized to
        /// the <c>[a-z0-9/._-]*</c> range normal datapacks support.
        /// </param>
        public MCFunctionFile(MCFunctionName path) {
            Path = path;
        }

        /// <summary>
        /// Returns a string containing the compiled datapack formatted as:
        /// <code>
        /// scoreboard player add blah blah blah
        /// say hi
        /// # etc...
        /// </code>
        /// </summary>
        public string GetContent() {
            if (code.Count == 0)
                return "# (Empty)";
            return string.Join("\n", code);
        }

        DatapackLocation IDatapackFile.DatapackLocation => DatapackLocation.Functions;
        string IDatapackFile.Namespace => Path.name.Split(':')[0];
        string IDatapackFile.Subpath => Path.name.Split(':')[1] + ".mcfunction";
        string IDatapackFile.GetFileContents() => GetContent();
    }
}
