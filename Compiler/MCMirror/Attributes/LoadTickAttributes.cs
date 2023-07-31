using MCMirror.Internal;
using System;

namespace MCMirror {
    /// <summary>
    /// <para>
    /// This attribute makes a method be called on load: whenever the world is
    /// (re)loaded, and whenever the datapack is (re)loaded.
    /// </para>
    /// <para>
    /// For a load that happens exactly once, use <see cref="TrueLoadAttribute"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is only valid on <c>static void</c> methods without arguments.
    /// </para>
    /// <para>
    /// Depending on your Minecraft version (supposedly up to 1.19.2 and fixed
    /// after), the <tt>tick</tt> function runs before <tt>load</tt>. Maybe
    /// this needs to be taken into account, but hasn't yet.
    /// See <a href="https://bugs.mojang.com/browse/MC-187539">MC-187539</a>
    /// for details.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class LoadAttribute : Attribute { }

    /// <summary>
    /// <para>
    /// This attributes makes a method be called on load only once. (It
    /// can of course be manually called.)
    /// </para>
    /// <para>
    /// For the attribute that happens every world/datapack (re)load, see
    /// <see cref="LoadAttribute"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="LoadAttribute"/>
    /// <para>
    /// Note that this method gets rerun on updated (i.e. recompiled) versions
    /// again. For even more "run only once", consider using Minecraft's
    /// storage.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class TrueLoadAttribute : Attribute { }

    /// <summary>
    /// <para>
    /// This attributes makes a method be called every tick, optionally with a
    /// bigger interval gap.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="LoadAttribute"/>
    /// <para>
    /// Note that there is no guarantee methods with the same argument get run
    /// the same time, or in what order. The compiler interleaves larger delays
    /// to reduce lagspikes. Due to this spread, not all such tick methods
    /// start at the load tick. (In fact, none do.) For more control, use one
    /// method with this attribute that calls a bunch more methods. 
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class TickAttribute : Attribute {
        public readonly int value;

        public TickAttribute() {
            value = 1;
        }

        /// <param name="value">
        /// How many ticks to wait between each call. Defaults to <tt>1</tt>.
        /// Twenty ticks a second.
        /// </param>
        public TickAttribute(int value) {
            this.value = value;
        }
    }
}