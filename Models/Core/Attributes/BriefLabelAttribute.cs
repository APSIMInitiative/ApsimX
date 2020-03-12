namespace Models.Core
{
    using System;

    /// <summary>
    /// Attribute to hold a short description string for a property
    /// This is almost identical to the DescriptionAttribute, but is intended
    /// to allow for a "brief" as well as a "lengthy" description. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class CaptionAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaptionAttribute" /> class.
        /// </summary>
        /// <param name="description">Description text</param>
        public CaptionAttribute(string description)
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
