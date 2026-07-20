using System;
using System.IO;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Storage
{

    /// <summary>
    /// A storage service for reading and writing to/from a database.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.DataStoreView.glade")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class DataStore : Model, IDataStore, IDisposable
    {
        /// <summary>A database connection</summary>
        [NonSerialized]
        private IDatabaseConnection _connection = null;

        [NonSerialized]
        private DataStoreReader _dbReader = new DataStoreReader();

        [NonSerialized]
        private DataStoreWriter _dbWriter = new DataStoreWriter();

        private bool _useInMemoryDB;

        [JsonIgnore]
        private string _fileName;

        /// <summary>
        /// Controls whether the database connection is an in-memory DB.
        /// </summary>
        [JsonIgnore]
        public bool UseInMemoryDB
        {
            get
            {
                return _useInMemoryDB;
            }
            set
            {
                _useInMemoryDB = value;
                UpdateFileName();
            }
        }

        /// <summary>
        /// Returns the file name of the .db file.
        /// Returns CustomFileName if it has been given; will fallback to
        /// fileName otherwise.
        /// </summary>
        [JsonIgnore]
        public string FileName
        {
            get
            {
                return string.IsNullOrWhiteSpace(CustomFileName) ? _fileName : CustomFileName;
            }
            set
            {
                UpdateFileName(value);
            }
        }

        /// <summary>
        /// Allows the user to override the .db file location.
        /// </summary>
        public string CustomFileName { get; set; } = null;

        /// <summary>Get a reader to perform read operations on the datastore.</summary>
        public IStorageReader Reader { get { return _dbReader; } }

        /// <summary>Get a writer to perform write operations on the datastore.</summary>
        public IStorageWriter Writer { get { return _dbWriter; } }

        /// <summary>Constructor</summary>
        public DataStore()
        {
        }

        /// <summary>Constructor</summary>
        public DataStore(string fileNameToUse)
        {
            _fileName = fileNameToUse;
            SQLite database = new SQLite();
            database.OpenDatabase(_fileName, true);
            _connection = database;
            _dbReader.SetConnection(_connection);
            _dbWriter.SetConnection(_connection);
        }

        /// <summary>Constructor</summary>
        public DataStore(IDatabaseConnection db)
        {
            _connection = db;
            _dbReader.SetConnection(_connection);
            _dbWriter.SetConnection(_connection);
        }

        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~DataStore()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed = false;

        /// <summary>Dispose method</summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
            Close();
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    // component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                Close();

                // Note disposing has been done.
                disposed = true;
            }
        }

        /// <summary>Object has been created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();

            if (_connection == null)
            {
                UpdateFileName();
                Open();
            }
        }

        /// <summary>
        /// Updates the file name of the database file, based on the file name
        /// of the parent Simulations object.
        /// </summary>
        public void UpdateFileName(string fileName = null)
        {
            bool connectionOpen = false;
            if (_connection != null)
                connectionOpen = true;
            
            if (connectionOpen)
                Close();

            if (_useInMemoryDB)
            {
                _fileName = ":memory:";
                if (connectionOpen)
                    Open();
                return;
            }

            if (Node == null)
            {
                _fileName = "";
                if (connectionOpen)
                    Open();
                return;
            }
            
            if (fileName != null)
            {
                _fileName = fileName;
                if (connectionOpen)
                    Open();
                return;
            }

            Simulations simulations = Node.FindParent<Simulations>(recurse: true);

            // If we have been cloned prior to a run, then we won't be able to locate
            // the simulations object. In this situation we can fallback to using the
            // parent simulation's filename (which should be the same anyway).
            Simulation simulation = Node.FindParent<Simulation>(recurse: true);

            string extension = ".db";
            if (simulations != null && simulations.FileName != null)
                _fileName = Path.ChangeExtension(simulations.FileName, extension);
            else if (simulation != null && simulation.FileName != null)
                _fileName = Path.ChangeExtension(simulation.FileName, extension);
            else
                _fileName = ":memory:";

            if (connectionOpen)
                Open();
        }

        /// <summary>Open the database.</summary>
        private void Open()
        {
            _connection = new SQLite();
            _connection.OpenDatabase(FileName, readOnly: false);

            Exception caughtException = null;
            try
            {
                if (_dbReader == null)
                    _dbReader = new DataStoreReader();
                _dbReader.SetConnection(_connection);
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            try
            {
                if (_dbWriter == null)
                    _dbWriter = new DataStoreWriter();
                _dbWriter.SetConnection(_connection);
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            if (caughtException != null)
                throw caughtException;
        }

        /// <summary>Close the database.</summary>
        public void Close()
        {
            if (_connection != null)
            {
                _connection.CloseDatabase();
                _connection = null;
            }
        }

        /// <inheritdoc/>
        public void AddView(string name, string selectSQL)
        {
            if (_connection is SQLite)
            {
                if (_connection.ViewExists(name))
                {
                    _connection.ExecuteNonQuery($"DROP VIEW {name}");
                }
                _connection.ExecuteNonQuery($"CREATE VIEW {name} AS {selectSQL}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public string GetViewSQL(string name)
        {
            if (_connection is SQLite)
            {
                if (_connection.ViewExists(name))
                {
                    var resultSql = _dbReader.GetDataUsingSql($"SELECT sql FROM sqlite_master WHERE type='view' and name='{name}'");
                    if (resultSql.Rows.Count > 0)
                        return resultSql.Rows[0].ItemArray[0].ToString();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return "";
        }

        /// <summary>
        /// Wait until writing has finished to the database, then find and 
        /// refresh all the datastores in the file.
        /// </summary>
        public void Refresh()
        {
            if (Node == null)
            {
                //used by unit tests where datastore isnt in a simulation
                Writer.WaitForIdle();
                Reader.Refresh();
            }
            else
            {
                IModel rootModel = Node.Root().Model as IModel;
                foreach (IDataStore datastore in rootModel.Node.FindAll<IDataStore>())
                    if (datastore.Writer != null)
                        datastore.Writer.WaitForIdle();
                foreach (IDataStore datastore in rootModel.Node.FindAll<IDataStore>())
                    if (datastore.Reader != null)
                        datastore.Reader.Refresh();
            }
        }
    }
}