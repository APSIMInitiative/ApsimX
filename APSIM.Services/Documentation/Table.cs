using System.Data;

namespace APSIM.Services.Documentation
{
    /// <summary>Describes an auto-doc table command.</summary>
    public class Table : ITag
    {
        /// <summary>The data to show in the table.</summary>
        private DataView data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        public Table(DataView data)
        {
            this.data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        public Table(DataTable data)
        {
            this.data = new DataView(data);
        }
    }
}
