using System.Collections.Generic;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// Extends the IGraphable interface by allowing for the use of cached input data from another model.
    /// </summary>
    /// <remarks>
    /// Some IGraphable implementations (such as Regression) display metadata about data displayed
    /// by another model. In this case, it is convenient to reuse cached data rather than read
    /// the data from an IStorageReader (e.g. a database) more than once.
    /// 
    /// This interface defines a common specification for such models.
    /// 
    /// Note that implementators should still provide a working implementation of IGraphable,
    /// for scenarios where cached data is unavailable.
    /// </remarks>
    public interface ICachableGraphable : IGraphable
    {
        /// <summary>Get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">Cached series definitions.</param>
        /// <param name="simulationsFilter">Simulation names filter.</param>
        /// <param name="storage">Data retrieval service.</param>
        IEnumerable<SeriesDefinition> GetSeriesToPutOnGraph(IStorageReader storage, IEnumerable<SeriesDefinition> definitions, List<string> simulationsFilter = null);
    }
}
