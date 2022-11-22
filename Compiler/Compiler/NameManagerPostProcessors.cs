using System.Collections.Generic;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// An interface that mangles the output of NameManager variable names for
    /// whatever purpose.
    /// </summary>
    public interface INameManagerPostProcessor {
        public string PostProcess(string name);
    }

    public class NamePostProcessors {

        /// <summary>
        /// Leaves the output intact. Good for debug builds.
        /// </summary>
        public class Identity : INameManagerPostProcessor {
            public string PostProcess(string name) => name;
        }

        /// <summary>
        /// Minimises the output. Good for release builds.
        /// </summary>
        public class Minify : INameManagerPostProcessor {
            static readonly Dictionary<string, string> minification = new();
            // Too lazy to do ascii arithmetic
            static readonly string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            static readonly int numberBase = chars.Length;
            static readonly StringBuilder builder = new(capacity: 6); // prefix + 5 chars => 14M options

            public string PostProcess(string name) {
                if (minification.TryGetValue(name, out string ret))
                    return ret;

                builder.Clear();
                builder.Append('_');
                int num = minification.Count; // The empty string is fine if prefixed.
                while (num > 0) {
                    builder.Append(chars[num % numberBase]);
                    num /= numberBase;
                }
                ret = builder.ToString();
                minification.Add(name, ret);
                return ret;
            }

            /// <summary>
            /// Resets the minification table, which will introduce garbage
            /// if done at any time *during* the compilation.
            /// </summary>
            public static void Reset() {
                minification.Clear();
            }
        }
    }
}
