using System;

namespace Models.Core.Attributes
{
    /// <summary>
    /// Model version attribute
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class VersionAttribute : Attribute
    {
        private int major;
        private int minor;
        private int increment;
        private string comments;

        /// <summary>
        /// Attribute constructor
        /// </summary>
        /// <param name="major">Version major component</param>
        /// <param name="minor">Version minor component</param>
        /// <param name="increment">Minor increment</param>
        /// <param name="comments">Version comments</param>
        public VersionAttribute(int major, int minor, int increment, string comments)
        {
            this.comments = comments;
            this.major = major;
            this.minor = minor;
            this.increment = increment;
        }

        /// <summary>
        /// Gets the uri
        /// </summary>
        /// <returns>help link uri</returns>
        public override string ToString()
        {
            return major.ToString() + "." + minor.ToString() + "." + increment.ToString();
        }

        /// <summary>
        /// Get the comments associated with this version
        /// </summary>
        /// <returns>Comments</returns>
        public string Comments()
        {
            return comments;
        }

    }
}
