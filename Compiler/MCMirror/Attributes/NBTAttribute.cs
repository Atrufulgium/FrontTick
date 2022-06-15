using System;

namespace MCMirror {
    /// <summary>
    /// This attribute signifies data that is not stored by the compiled c#
    /// code, but instead by Minecraft itself, in (block)entities and storage.
    /// </summary>
    // "Method" arguably makes sense for write-only.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class NBTAttribute : Attribute {

        public readonly string path;

        /// <param name="path">
        /// <para>
        /// The path to the NBT data, as you would use in-game in
        /// <c>/data get [block|entity|storage] &lt;path&gt;</c>.
        /// </para>
        /// <para>
        /// Note: for custom mobs, this is not supported as Minecraft does not
        /// serialize their custom NBT.
        /// </para>
        /// </param>
        public NBTAttribute(string path) {
            this.path = path;
        }
    }
}