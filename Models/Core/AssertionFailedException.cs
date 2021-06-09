using System;

namespace Models.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class AssertionFailedException : Exception
    {
        /// <summary>
        /// Create a new <see cref="AssertionFailedException"/> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        public AssertionFailedException(string message) : base(message)
        {
        }
    }
}