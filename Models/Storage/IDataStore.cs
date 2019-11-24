namespace Models.Storage
{
    using Models.Core;
    using System;

    /// <summary>An interface  for reading and writing to/from a database.</summary>
    public interface IDataStore : IDisposable
    {
        /// <summary>Gets or sets the file name of the file to write to.</summary>
        string FileName { get; }

        /// <summary>Get a reader to perform read operations on the datastore.</summary>
        IStorageReader Reader { get; }

        /// <summary>Get a writer to perform write operations on the datastore.</summary>
        IStorageWriter Writer { get; }

        /// <summary>Opens the database connection.</summary>
        void Open();

        /// <summary>Closes the database connection.</summary>
        void Close();
    }
}