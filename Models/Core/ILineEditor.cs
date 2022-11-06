using System.Collections.Generic;

namespace Models.Core
{
    /// <summary>
    /// An interface for a model which is a test.
    /// </summary>
    public interface ILineEditor : IModel
    {
        /// <summary>
        /// Runs the test. Throws an exception on failure.
        /// </summary>
        IEnumerable<string> Lines { get; set; }
    }
}
