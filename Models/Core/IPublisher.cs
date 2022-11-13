namespace Models.Core
{
    using System;
    using System.Collections.Generic;

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