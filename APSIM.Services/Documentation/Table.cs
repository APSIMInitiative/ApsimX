using System.Data;

namespace APSIM.Services.Documentation
{
    /// <summary>Describes an auto-doc table command.</summary>
    public class Table : Tag
    {
        /// <summary>The data to show in the table.</summary>
        private DataView data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        /// <param name="indent">Indentation level.</param>
        public Table(DataView data, int indent = 0) : base(indent)
        {
            this.data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">Data to be displayed in the table.</param>
        /// <param name="indent">Indentation level.</param>
        public Table(DataTable data, int indent = 0) : base(indent)
        {
            this.data = new DataView(data);
        }
    }
}
