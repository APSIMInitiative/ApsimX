using System.Collections.Generic;

namespace Models.Core
{

    /// <summary>
    /// Interface for a model which publishes events whose
    /// names are not known at compile-time.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Names of the published events.
        /// </summary>
        IEnumerable<string> Events { get; }
    }
}