﻿namespace MCMirror {
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

        public int3(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static int3 operator +(int3 a, int3 b) {
            a.x += b.x;
            a.y += b.y;
            a.z += b.z;
            return a;
        }

        public static bool operator ==(int3 a, int3 b)
            => a.x == b.x & a.y == b.y & a.z == b.z;

        public static bool operator !=(int3 a, int3 b)
            => a.x != b.x | a.y != b.y | a.z != b.z;
    }
}