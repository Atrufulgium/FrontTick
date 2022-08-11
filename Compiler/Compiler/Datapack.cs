using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Represents the full codebase of a Minecraft datapack.
    /// </summary>
    public class Datapack {
        // Keep them sorted alphabetically by path to keep the string output
        // consistent. Maybe it even helps with the filesystem output.
        public SortedSet<DatapackFile> files = new(Comparer<DatapackFile>.Create((a,b) => a.Path.CompareTo(b.Path)));

        public Datapack() { }
        public Datapack(IEnumerable<DatapackFile> files) {
            foreach (var file in files)
                this.files.Add(file);
        }

        /// <summary>
        /// Write this datapack's code to the specified folder.
        /// </summary>
        public void WriteToFilesystem(string rootPath) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write this datapack's code to a large string of the form:
        /// <code>
        /// # (File namespace:filename1.mcfunction)
        /// scoreboard player add blah blah blah
        /// 
        /// # (File namespace:filename2.mcfunction)
        /// say hi
        /// # More commands, more files, etc.
        /// </code>
        /// </summary>
        public override string ToString()
            => string.Join("\n\n", files);

        /// <summary>
        /// Write this datapack's code to a large string of the form:
        /// <code>
        /// # (File namespace:filename1.mcfunction)
        /// scoreboard player add blah blah blah
        /// function namespace:filename1-lorem
        ///     # (File namespace:filename1-lorem.mcfunction)
        ///     say lorem
        ///     function namespace:filename1-lorem-ipsum
        ///         # (File namespace:filename1-lorem-ipsum.mcfunction)
        ///         say ipsum
        ///     say dolor sit amet
        /// # More commands in filename1.mcfunction
        /// 
        /// # (File namespace:filename2.mcfunction)
        /// say hi
        /// # More commands, more files, etc.
        /// </code>
        /// More specifically, it nests a function's code into the first
        /// occurance of it being called if that function's name starts with
        /// the calling functions name, instead of being printed at root.
        /// </summary>
        /// <remarks>
        /// This is to be used purely for debugging purposes and not for
        /// checking whether tests work as expected. For that, use
        /// <see cref="ToString"/>.
        /// </remarks>
        public string ToTreeString() {
            string result = "";
            processedFunctions = new();
            functionsByName = files.ToDictionary(file => file.Path, file => file);

            foreach (var file in files) {
                if (!processedFunctions.Contains(file.Path))
                    result += $"\n{GetTreeFunctionAtDepth(file, file.Path, 0)}";
            }
            return result.Trim();
        }

        HashSet<MCFunctionName> processedFunctions;
        Dictionary<MCFunctionName, DatapackFile> functionsByName;
        private string GetTreeFunctionAtDepth(DatapackFile file, MCFunctionName topName, int depth) {
            processedFunctions.Add(file.Path);
            var indent = new string(' ', 4 * depth);
            // Header structure copypasta'd from
            /// <see cref="DatapackFile.ToString"/>
            var result = $"\n{indent}# (File {file.Path}.mcfunction)";
            if (file.code.Count == 0)
                result += $"\n{indent}# (Empty)";
            foreach (var line in file.code) {
                // Check if this file calls another function
                string function = null;
                if (line.StartsWith("function"))
                    function = line[9..]; // "function " takes up 9 chars.
                else {
                    int index = line.IndexOf("run function");
                    if (index >= 0)
                        function = line[(index + 13)..]; // "run function " takes up 13 chars.
                }
                // If this file calls another function, embed it after printing this line.
                result += $"\n{indent}{line}";
                if (function != null) {
                    // Yes I'm violating my own constructor requirement of "don't instantiate!"
                    // oops
                    // At least this is the result of a roundtrip MCFunction -> string -> MCFunction,
                    // so it cannot go wrong.
                    MCFunctionName nested = new(function);
                    if (nested.name.StartsWith(topName.name)
                        && !processedFunctions.Contains(nested)
                        && functionsByName.TryGetValue(nested, out var nestedFile))
                        result += GetTreeFunctionAtDepth(nestedFile, topName, depth + 1);
                }
            }
            return result;
        }
    }
}
