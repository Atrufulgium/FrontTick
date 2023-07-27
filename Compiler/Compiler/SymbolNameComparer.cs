using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// Symbol equality is kinda whack when you edit the tree: everything turns
    /// false. This does not suffer from that by instead comaring the <i>string
    /// values</i> that the symbols represent.
    /// </para>
    /// <para>
    /// This is not foolproof: this can give false positives if the tree is
    /// malformed resulting in multiple symbols with the same fully qualified
    /// name, but that is better than everything being <tt>false</tt> with
    /// the provided comparers.
    /// </para>
    /// </summary>
    internal class SymbolNameComparer : IEqualityComparer<ISymbol> {
        public bool Equals(ISymbol x, ISymbol y) {
            if (x == null)
                return y == null;
            return x.ToString().Equals(y?.ToString());
        }

        public int GetHashCode([DisallowNull] ISymbol obj)
            => obj.ToString().GetHashCode();
    }
}
