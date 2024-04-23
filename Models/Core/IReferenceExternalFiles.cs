using System.Collections.Generic;

namespace Models.Core
{

    /// <summary>An interface for a model that references external files</summary>
    public interface IReferenceExternalFiles : IModel
    {
        /// <summary>Return paths to all files referenced by this model.</summary>
        IEnumerable<string> GetReferencedFileNames();

        /// <summary>Remove all paths from referenced filenames.</summary>
        void RemovePathsFromReferencedFileNames();
    }
}
