using System.Collections.Generic;
using Models.Utilities;
using System.Data;


namespace Models.Interfaces
{
    /// <summary>An interface for editable tabular data.</summary>
    public interface IGridModel
    {
        /// <summary>Get tabular data. Called by GUI.</summary>
        public List<GridTable> Tables { get; }

        /// <summary>
        /// A Description of the Model displayed in the Grid view
        /// Default is a blank string, this can be overridden by a model class</summary>
        public string GetDescription() {
            return "";
        }

        /// <summary>
        /// Called by Presenter and allow classes to modify the data table to change how its displayed.
        /// This function converts from the datatable built from the model, to the datatable that will be displayed.
        /// By Default no conversion happens.
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            return dt;
        }

        /// <summary>
        /// Called by Presenter and allow classes to modify the data table to change how its displayed.
        /// This function converts from the datatable shown in the display, to the datatable is used to store the data.
        /// By Default no conversion happens.
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            return dt;
        }
    }
}
