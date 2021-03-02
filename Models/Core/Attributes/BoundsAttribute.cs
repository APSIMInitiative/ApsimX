namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies the lower and upper bounds for the related field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BoundsAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the lower bound
        /// </summary>
        public double Lower { get; set; }

        /// <summary>
        /// Gets or sets the upper bound
        /// </summary>
        public double Upper { get; set; }
    }
}
