using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Contains a compiled file ready for datapack insertion.
    /// </summary>
    public class DatapackFile {
        /// <summary>
        /// The relative location of this file, excluding the
        /// <c>.mcfunction</c> suffix and namespace prefix.
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
        public DatapackFile(MCFunctionName path) {
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
        public string GetContent()
            => string.Join("\n", code);

        /// <summary>
        /// Returns a string containing the compiled datapack formatted as:
        /// <code>
        /// # (File &lt;namespace:filename.mcfunction&gt;)
        /// scoreboard player add blah blah blah
        /// say hi
        /// # etc...
        /// </code>
        /// </summary>
        public override string ToString() {
            if (code.Count > 0)
                return $"# (File {Path}.mcfunction)\n{GetContent()}";
            return $"# (File {Path}.mcfunction)\n# (Empty)";
        }
    }
}
