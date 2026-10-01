using System.Collections.Generic;

namespace Models.Core
{
    /// <summary>
    /// An interface for a model which is a test.
    /// </summary>
    public interface ICodeEditor : IModel
    {
        /// <summary>
        /// Runs the test. Throws an exception on failure.
        /// </summary>
        IEnumerable<string> Code { get; set; }
    }
}
