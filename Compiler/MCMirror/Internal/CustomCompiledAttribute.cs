using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This attribute signifies a method that is not to be compiled normally,
    /// but instead via some custom compiler implementation.
    /// The method body is *fully* ignored.
    /// </para>
    /// <para>
    /// The <see cref="name"/> specifies what it is known to to the compiler
    /// and the NameManager. This name is encouraged to use illegal datapack
    /// names to prevent conflict, such as uppercase, #, etc.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class CustomCompiledAttribute : Attribute {
        public readonly string name;
        public CustomCompiledAttribute() {
            name = null;
        }
        public CustomCompiledAttribute(string name) {
            this.name = name;
        }
    }
}