using Microsoft.CodeAnalysis;
using System;
using System.Collections.ObjectModel;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// A visitor that does nothing at all (and instead is valuable due to its
    /// generic arguments specifying the dependencies in the same way as
    /// <see cref="AbstractFullWalker"/> and <see cref="AbstractFullRewriter"/>)
    /// </summary>
    public abstract class AbstractCategory : IFullVisitor {
        bool IFullVisitor.ReadOnly => true;
        int IFullVisitor.DependencyDepth { get; set; }

        ReadOnlyCollection<Diagnostic> ICustomDiagnosable.CustomDiagnostics => new(Array.Empty<Diagnostic>());
        void ICustomDiagnosable.AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs) {
            throw new NotSupportedException("This is a category. What are you doing requiring exceptions?");
        }

        void IFullVisitor.FullVisit() { }
        void IFullVisitor.SetCompiler(Compiler c) { }
    }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1>
        : AbstractCategory
        where TDep1 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2>
        : AbstractCategory<TDep1>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3>
        : AbstractCategory<TDep1, TDep2>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4>
        : AbstractCategory<TDep1, TDep2, TDep3>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5>
        : AbstractCategory<TDep1, TDep2, TDep3, TDep4>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6>
        : AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7>
        : AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8>
        : AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor
        where TDep8 : IFullVisitor { }

    /// <inheritdoc/>
    public abstract class AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8, TDep9>
        : AbstractCategory<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor
        where TDep8 : IFullVisitor
        where TDep9 : IFullVisitor { }
}
