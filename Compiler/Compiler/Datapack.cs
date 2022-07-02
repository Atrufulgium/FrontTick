using System;
using System.Collections.Generic;

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
        /// # (File &lt;namespace:filename1.mcfunction&gt;)
        /// scoreboard player add blah blah blah
        /// 
        /// # (File &lt;namespace:filename2.mcfunction&gt;)
        /// say hi
        /// # More commands, more files, etc.
        /// </code>
        /// </summary>
        public override string ToString()
            => string.Join("\n\n", files);
    }
}
