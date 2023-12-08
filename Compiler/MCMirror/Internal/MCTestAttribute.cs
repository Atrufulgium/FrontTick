using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This attribute signifies that a method is a test method to be run
    /// inside Minecraft. These methods can be run by with a function tag,
    /// <tt>/function #&lt;namespace&gt;:test</tt>.
    /// </para>
    /// <para>
    /// To keep me from having a headache, at this point testing boils down to
    /// checking whether the method's integer return value matches the literal.
    /// </para>
    /// <para>
    /// Only valid on methods of signature <tt>static int(void)</tt>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [CompilerUsesName]
    [NoCompile]
    public class MCTestAttribute : Attribute {

        public readonly int value;

        public MCTestAttribute(int value) {
            this.value = value;
        }
    }
}