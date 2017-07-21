// -----------------------------------------------------------------------
// <copyright file="ReportTables.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A class for holding a collection of tables. Each table will only exist once in the collection
    /// i.e. table name will be a unique key.
    /// </summary>
    [Serializable]
    public partial class ReportTables
    {
        /// <summary>Internal list of tables.</summary>
        private List<ReportTable> tables = new List<ReportTable>();

        /// <summary>Write all data to external file.</summary>
        public bool ExternalData { get; set; }

        /// <summary>Get an enumeration of tables.</summary>
        public IEnumerable<ReportTable> Tables { get { return tables; } }

        /// <summary>Add a new table.</summary>
        /// <param name="table">The table to add.</param>
        public void Add(ReportTable table)
        {
            lock (this)
            {
                ReportTable ourTable = tables.Find(t => t.TableName == table.TableName);
                if (ourTable == null)
                {
                    tables.Add(table);
                    ourTable = table;
                }
                else
                    ourTable.Merge(table);

                if (ExternalData)
                    ourTable.WriteDataToDisk();
            }
        }

        /// <summary>Clear the list of tables.</summary>
        public void Clear()
        {
            tables.Clear();
        }
    }
}
