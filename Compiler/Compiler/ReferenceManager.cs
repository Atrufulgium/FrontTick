using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// Handles the creation of references to assemblies, which by default
    /// include a few assemblies without which things quite frankly just break.
    /// </para>
    /// <para>
    /// Its only method is <see cref="GetReferences(IEnumerable{Type})"/> with
    /// overloads.
    /// </para>
    /// </summary>
    internal static class ReferenceManager {

        /// <summary>
        /// Creates references to all assemblies that contain <paramref name="additionalTypes"/>
        /// with a few extra assemblies (like System) that any project needs.
        /// </summary>
        public static HashSet<MetadataReference> GetReferences(IEnumerable<Type> additionalTypes) {
            if (additionalTypes == null) {
                additionalTypes = Array.Empty<Type>();
            }
            var references = TypesToAssemblyReferences(additionalTypes);
            references.UnionWith(autoInclude);
            return references;
        }
        /// <summary>
        /// Simply returns the union of the given references and a few extra
        /// assemblies (like System) that any project needs.
        /// </summary>
        public static HashSet<MetadataReference> GetReferences(IEnumerable<MetadataReference> additionalreferences) {
            if (additionalreferences == null) {
                additionalreferences = Array.Empty<MetadataReference>();
            }
            var references = additionalreferences.ToHashSet();
            references.UnionWith(autoInclude);
            return references;
        }

#pragma warning disable IDE0001, IDE0049
        /// <summary>
        /// A list of types whose assemblies to automatically include in any
        /// compilation. The actual listed types don't matter, just their
        /// containing assembly.
        /// </summary>
        // Until the far future, this is all that's supported anyway.
        static readonly Type[] autoIncludeAssemblyTypes = new[] {
            typeof(System.Object)
        };
#pragma warning restore IDE0001, IDE0049
        /// <summary>
        /// Unfortunately, getting assemblies via types isn't sufficient.
        /// There are a few literal dlls we need to read that don't seem to be
        /// able to be accessed in another way.
        /// These are their filenames (without the .dll), assuming they live
        /// in the same folder as System (which is a pretty fair assumption).
        /// </summary>
        static readonly string[] difficultAutoIncludeDLLNames = new[] {
            "netstandard",          // This one seems fair
            "System.Runtime"        // For System.Attribute, somewhy
        };

        static readonly HashSet<MetadataReference> autoInclude;

        static ReferenceManager() {
            var references = PrepareDifficultMetadataReferences();
            references.UnionWith(TypesToAssemblyReferences(autoIncludeAssemblyTypes));
            autoInclude = references;
        }

        static HashSet<MetadataReference> TypesToAssemblyReferences(IEnumerable<Type> types) {
            HashSet<MetadataReference> references = new();
            foreach (var assemblyType in types) {
                var assembly = MetadataReference.CreateFromFile(assemblyType.Assembly.Location);
                references.Add(assembly);
            }
            return references;
        }

        /// <summary>
        /// A few assemblies are annoying and need to manually be added.
        /// Only call this once, in the static constructor.
        /// </summary>
        static HashSet<MetadataReference> PrepareDifficultMetadataReferences() {
            // See https://stackoverflow.com/a/39049422
            var assemblyLocation = typeof(object).Assembly.Location;
            var coreDir = System.IO.Directory.GetParent(assemblyLocation);

            HashSet<MetadataReference> difficultReferences = new();
            foreach (var a in difficultAutoIncludeDLLNames) {
                difficultReferences.Add(MetadataReference.CreateFromFile(
                    $"{coreDir.FullName}{System.IO.Path.DirectorySeparatorChar}{a}.dll"
                    )
                );
            }
            return difficultReferences;
        }
    }
}
