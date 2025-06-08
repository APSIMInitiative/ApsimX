
using Models.Storage;
using System;

namespace Models.Core
{
    /// <summary>
    /// A simulation model
    /// </summary>
    public class Metadata
    {
        private IDataStore datastore = null;

        /// <summary></summary>
        public Metadata(IDataStore datastore)
        {
            this.datastore = datastore;
        }

        /// <summary></summary>
        public void OnSowing(object sender, EventArgs e)
        {
            return;
        }
    }
}