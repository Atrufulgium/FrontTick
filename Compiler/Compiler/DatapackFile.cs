using System.Collections.Generic;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Contains a compiled file ready for datapack insertion.
    /// </summary>
    public class DatapackFile {
        /// <summary>
        /// The relative location of this file, excluding the
        /// <c>.mcfunction</c> suffix and namespace prefix.
        /// </summary>
        public string Path { get; private set; }
        /// <summary>
        /// The mcfunction namespace this file lives in.
        /// </summary>
        public string Namespace { get; private set; }
        /// <summary>
        /// All code, separated line by line for easy additions.
        /// </summary>
        public List<string> code = new();

        /// <param name="path">
        /// The relative location of this file, excluding the
        /// <c>.mcfunction</c> suffix. This is normalized to the
        /// <c>[a-z0-9/._-]*</c> range normal datapacks support via
        /// <see cref="NormalizeFunctionName(string)"/>.
        /// </param>
        public DatapackFile(string path, string manespace) {
            Path = $"{NormalizeFunctionName(path)}.mcfunction";
            Namespace = manespace;
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
        public override string ToString()
            => $"# (File {Namespace}:{Path})\n{GetContent()}";

        /// <summary>
        /// This normalizes strings to the <c>[a-z0-9/._-]*</c> range normal
        /// datapack filenames support by lowercasing the letters, replacing
        /// spaces with underscores, and discarding the rest. There is no check
        /// as to whether this is sensible!
        /// </summary>
        public static string NormalizeFunctionName(string str) {
            StringBuilder builder = new StringBuilder(str.Length);
            foreach (char c in str) {
                if (('a' <= c && c <= 'z')
                    || ('0' <= c && c <= '9')
                    || c == '/' || c == '.'
                    || c == '_' || c == '-') {
                    builder.Append(c);
                } else if ('A' <= c && c <= 'Z') {
                    builder.Append((char)(c - 'A' + 'a'));
                } else if (c == ' ') {
                    builder.Append('_');
                }
                // Otherwise don't append anything and discrd this char.
            }
            return builder.ToString();
        }
    }
}
