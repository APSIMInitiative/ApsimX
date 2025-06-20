using System;

namespace Models.Core
{

    /// <summary>
    /// This abstract base class encapsulates the interface for a variable from a Model.
    /// source code.
    /// </summary>
    [Serializable]
    public abstract class IVariable
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets or sets the object this variable is relative to
        /// </summary>
        public abstract object Object { get; set; }

        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public abstract Type DataType { get; }

        /// <summary>
        /// Gets a description of the property or null if not found.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets or sets the units of the property or null if not found.
        /// </summary>
        public abstract string Units { get; set; }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public abstract string UnitsLabel { get; }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public abstract bool Writable { get; }

        /// <summary>Return the summary comments from the source code.</summary>
        public abstract string Summary { get; }

        /// <summary>Return the remarks comments from the source code.</summary>
        public abstract string Remarks { get; }
    }
}
