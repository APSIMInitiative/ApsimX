namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies a Uri for the help link in ModelDetailsWrapperView
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class HelpUriAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string helpUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpUriAttribute" /> class.
        /// </summary>
        /// <param name="uri">Description text</param>
        public HelpUriAttribute(string uri)
        {
            this.helpUri = uri;
        }

        /// <summary>
        /// Gets the uri
        /// </summary>
        /// <returns>help link uri</returns>
        public override string ToString()
        {
            return this.helpUri;
        }

    }
}
