using System.Collections.Generic;
using System.Text;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// An interface that mangles the output of NameManager variable names for
    /// whatever purpose.
    /// </summary>
    public interface INameManagerPostProcessor {
        // TODO: this is a bad abstraction, just create something for (selector, scoreboard) pairs
        /// <summary>
        /// Post processes all variable names.
        /// </summary>
        public string PostProcessVariable(string name);
        /// <summary>
        /// Post processes all file names. This implicitely assumes that the
        /// output is a valid datapack file name if the input is.
        /// </summary>
        public string PostProcessFunction(string name);
    }

    public class NamePostProcessors {

        /// <summary>
        /// Leaves the output intact. Good for debug builds.
        /// </summary>
        public class Identity : INameManagerPostProcessor {
            public string PostProcessVariable(string name) => name;
            public string PostProcessFunction(string name) => name;
        }

        /// <summary>
        /// Minimises the output. Good for release builds.
        /// Leaves functions intact currently because NameManager is *bad*.
        /// </summary>
        public class Minify : INameManagerPostProcessor {
            static readonly Dictionary<string, string> minification = new();
            // Too lazy to do ascii arithmetic
            static readonly string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            static readonly int numberBase = chars.Length;
            static readonly StringBuilder builder = new(capacity: 6); // prefix + 5 chars => 14M options

            public string PostProcessVariable(string name) {
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

            public string PostProcessFunction(string name) => name;

            /// <summary>
            /// Resets the minification table, which will introduce garbage
            /// if done at any time *during* the compilation.
            /// </summary>
            public static void Reset() {
                minification.Clear();
            }
        }

        /// <summary>
        /// Removes # and - from variable and method names so comparison of
        /// generated code to regular code can be done.
        /// </summary>
        /// <remarks>
        /// Exception: *double* `--`s in methods are not replaced. These are so
        /// internal that they should not even be needed to be emulated in
        /// any tests.
        /// </remarks>
        public class ConvenientTests : INameManagerPostProcessor {
            public string PostProcessVariable(string name)
                => name.Replace("#", "").Replace("-", "");
            public string PostProcessFunction(string name)
                => name.Replace("#", "").Replace("--", "**").Replace("-", "").Replace("**", "--");
        }
    }
}
