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
    public class MCFunctionAttribute : Attribute {
        /// <summary>
        /// <para>
        /// If present, this string specifies (excluding namespace) the name of
        /// this function in the datapack, i.e., it will be called as
        /// <code>
        /// /function namespace:name
        /// </code>
        /// (with the namespace specified during compile-time).
        /// </para>
        /// <para>
        /// If absent, the function name will simply be <c>class.method</c>.
        /// </para>
        /// </summary>
        public string name;
    }
}