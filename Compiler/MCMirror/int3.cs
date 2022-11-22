namespace MCMirror {
    /// <summary>
    /// <para>
    /// This is simply a 3-dimensional integer vector.
    /// </para>
    /// <para>
    /// Note that for absolute coordinates in Minecraft, +X is east, +Y is up,
    /// and +Z is north; it's right-handed.
    /// </para>
    /// </summary>
#pragma warning disable IDE1006 // Naming Styles
    public struct int3 {
#pragma warning restore IDE1006 // Naming Styles
        public int x;
        public int y;
        public int z;
    }
}