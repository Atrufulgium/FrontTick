using System.Collections.Generic;
using System.IO;

namespace Atrufulgium.FrontTick.Compiler {
    public static class FolderToContainingCode {
        /// <summary>
        /// <para>
        /// Recursively go through all children of the folder located at
        /// <paramref name="path"/> and an array of all their containing code.
        /// </para>
        /// <para>
        /// Each bit of code is tagged with a filepath in the second tuple index.
        /// </para>
        /// </summary>
        public static IEnumerable<(string code, string path)> GetCode(string path) {
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            List<(string,string)> code = new(files.Length);

            // If we call it on the full project, we get some useless files we ought to ignore.
            // Yes this is an unstable hack. Whatever.
            string vsDebugDirectory = @"\obj\Debug\".Replace('\\', Path.DirectorySeparatorChar);

            for (int i = 0; i < files.Length; i++) {
                if (files[i].Contains(vsDebugDirectory))
                    continue;
                code.Add((File.ReadAllText(files[i]), files[i]));
            }
            return code;
        }
    }
}
