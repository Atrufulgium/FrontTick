using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler.Datapack {
    public class FunctionTag : IDatapackFile {

        /// <summary>
        /// Snapshots the current list of tagged functions.
        /// </summary>
        public ReadOnlyCollection<MCFunctionName> TaggedFunctions => new(taggedFunctions.ToList());
        readonly ICollection<MCFunctionName> taggedFunctions;

        public FunctionTag(string manespace, string subpath, bool sorted = true) {
            if (!subpath.Contains('.'))
                throw new ArgumentException("The subpath must include a file extension", nameof(subpath));

            Namespace = manespace;
            Subpath = subpath;
            if (sorted)
                taggedFunctions = new SortedSet<MCFunctionName>(Comparer<MCFunctionName>.Create((a, b) => a.name.CompareTo(b.name)));
            else
                taggedFunctions = new List<MCFunctionName>();
        }

        public void AddToTag(MCFunctionName function) {
            taggedFunctions.Add(function);
        }

        public DatapackLocation DatapackLocation => DatapackLocation.FunctionTags;

        public string Namespace { get; init; }
        public string Subpath { get; init; }

        public string GetFileContents() {
            // (Note: Minecraft complains if there is no `values` key)
            if (taggedFunctions.Count == 0)
                return @"{""// (Empty)"": """", ""values"": []}";

            // yeh yeh i know handwriting json is taboo. meh.
            StringBuilder ret = new(32);
            ret.Append("{\"values\":[");
            foreach (var f in taggedFunctions)
                ret.Append($"\n  \"{f}\",");
            // This JSON does not allow trailing commas.
            ret.Remove(ret.Length - 1, 1);
            ret.Append("\n]}");
            return ret.ToString();
        }

        public override string ToString() => throw new InvalidOperationException();
    }
}
