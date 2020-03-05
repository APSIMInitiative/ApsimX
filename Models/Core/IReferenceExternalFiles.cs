namespace Models.Core
{
    using System.Collections.Generic;

    /// <summary>An interface for a model that references external files</summary>
    public interface IReferenceExternalFiles
    {
        /// <summary>Run tests. Should throw an exception if the test fails.</summary>
        IEnumerable<string> GetReferencedFileNames();
    }
}
