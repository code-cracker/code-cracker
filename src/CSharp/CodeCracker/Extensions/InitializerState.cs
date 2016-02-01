using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker
{
    /// <summary>
    /// Used to indicate what can be said about the initialization
    /// of a symbol in a given block of statements.
    /// </summary>
    public enum InitializerState
    {
        /// <summary>
        /// Indicates that the block of statements does NOT initialize the symbol for certain.
        /// </summary>
        None,
        /// <summary>
        /// Indicates that the block of statements DOES initialize the symbol for certain.
        /// </summary>
        Initializer,
        /// <summary>
        /// Indicates that the block of statements contains a way to skip any initializers
        /// following the given block of statements (for instance a return statement inside
        /// an if statement can skip any initializers after the if statement).
        /// </summary>
        WayToSkipInitializer,
    }
}
