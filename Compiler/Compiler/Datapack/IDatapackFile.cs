using System.Collections.Generic;
using System.IO;

namespace Atrufulgium.FrontTick.Compiler.Datapack {
    public interface IDatapackFile {
        /// <summary>
        /// <para>
        /// The datapack namespace this should be written in.
        /// </para>
        /// <para>
        /// For compiled functions this should usually be the provided
        /// namespace, but a lot of other things (such as function tags,
        /// shaders, etc) use the default <tt>minecraft</tt> namespace.
        /// </para>
        /// </summary>
        public string Namespace { get; }
        /// <summary>
        /// The type of file this is.
        /// </summary>
        public DatapackLocation DatapackLocation { get; }        
        /// <summary>
        /// The subpath of this file within its type folder. This is INCLUDING
        /// the file extension. For e.g. functions this could be
        /// "someFunction.mcfunction" or "dir/func.mcfunction".
        /// </summary>
        public string Subpath { get; }

        /// <summary>
        /// Write the contents of this datapack file to a single string.
        /// </summary>
        public string GetFileContents();
    }

    public static class DatapackFileExtensionMethods {

        static readonly char slash = Path.DirectorySeparatorChar;

        /// <summary>
        /// Write a datapack file to the file system.
        /// </summary>
        /// <param name="rootPath">
        /// The path to a datapack's directory, inside a Minecraft world. E.g.
        /// <tt>(.minecraft)/saves/(world)/datapacks/(datapack)</tt>
        /// </param>
        public static void WriteToFilesystem(this IDatapackFile file, string rootPath) {
            string fullPath = $"{rootPath}{slash}data{slash}{file.Namespace}{slash}{(string)file.DatapackLocation}{slash}{file.Subpath}";
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            
            using var fileContents = File.CreateText(fullPath);
            fileContents.Write(file.GetFileContents());
        }
    }

    /// <summary>
    /// Compares as follow:
    /// * First, group categories together.
    /// * Then, group namespaces together.
    /// * Finally, order alphabetically.
    /// </summary>
    public class DatapackFileComparer : IComparer<IDatapackFile> {
        public int Compare(IDatapackFile x, IDatapackFile y) {
            var v = ((string)x.DatapackLocation).CompareTo(y.DatapackLocation);
            if (v != 0)
                return v;
            v = x.Namespace.CompareTo(y.Namespace);
            if (v != 0)
                return v;
            // To maintain consistency with before, ignore the file extension
            // when comparing.
            string s1 = Path.GetFileNameWithoutExtension(x.Subpath);
            string s2 = Path.GetFileNameWithoutExtension(y.Subpath);
            return s1.CompareTo(s2);
        }

        public static DatapackFileComparer Comparer => new();
    }
}
