namespace Models.Core
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class DescriptionAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionAttribute" /> class.
        /// </summary>
        /// <param name="description">Description text</param>
        /// <param name="lineNum">Line number of declaration - inserted by compiler magically. Useful for sorting</param>
        public DescriptionAttribute(string description, [CallerLineNumber]int lineNum = 0)
        {
            this.description = description;
            LineNumber = lineNum;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        /// <returns>The description</returns>
        public override string ToString()
        {
            return this.description;
        }

        /// <summary>
        /// Line number of declaration
        /// </summary>
        public int LineNumber { get; private set; }
    } 
}
