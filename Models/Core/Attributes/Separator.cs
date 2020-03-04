namespace Models.Core
{
    using System;
 
    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class SeparatorAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatorAttribute" /> class.
        /// </summary>
        /// <param name="description">Description text</param>
        public SeparatorAttribute(string description)
        {
            this.description = description;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        /// <returns>The description</returns>
        public override string ToString()
        {
            return this.description;
        }
    } 
}
