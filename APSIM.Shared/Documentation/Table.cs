using System.Data;

namespace APSIM.Shared.Documentation
{
    /// <summary>Describes an auto-doc table command.</summary>
    public class Table : ITag
    {
        /// <summary>The data to show in the table.</summary>
        public DataTable Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        public Table(DataView data)
        {
            Data = data.ToTable();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        public Table(DataTable data)
        {
            Data = data;
        }
    }
}
