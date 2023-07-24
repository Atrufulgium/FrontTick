using MCMirror.Internal;
using System;

namespace MCMirror {
    /// <summary>
    /// <para>
    /// This attribute signifies an <c>.mcfunction</c> entrypoint that is
    /// intended to be called from within Minecraft with <c>/function</c>.
    /// </para>
    /// <para>
    /// You can specify <see cref="name"/> if you want to use a custom name for
    /// your entrypoint.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This attribute is only valid on <c>static void</c> methods without arguments.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class MCFunctionAttribute : Attribute {
        public readonly string name;
        public MCFunctionAttribute() {
            name = null;
        }
        /// <summary>
        /// Specify the custom name, nonempty using [a-z0-9/._-]-characters only.
        /// </summary>
        public MCFunctionAttribute(string name) {
            this.name = name;
        }
    }
}