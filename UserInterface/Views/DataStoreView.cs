// -----------------------------------------------------------------------
// <copyright file="DataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using System.Windows.Forms;

    /// <summary>The interface for a data store view</summary>
    public interface IDataStoreView
    {
        /// <summary>List of all tables.</summary>
        IDropDownView TableList { get; }

        /// <summary>Editable column filter.</summary>
        IEditView ColumnFilter { get; }

        /// <summary>Grid for holding data.</summary>
        IGridView Grid { get; }

        /// <summary>Maximum number of records.</summary>
        IEditView MaximumNumberRecords { get; }

    }

    /// <summary>
    /// A data store view
    /// </summary>
    public partial class DataStoreView : UserControl, IDataStoreView
    {
        /// <summary>Initializes a new instance of the <see cref="DataStoreView" /> class.</summary>
        public DataStoreView()
        {
            this.InitializeComponent();
        }

        /// <summary>List of all tables.</summary>
        public IDropDownView TableList { get { return dropDownView1; } }

        /// <summary>Editable column filter.</summary>
        public IEditView ColumnFilter { get { return editView1; } }

        /// <summary>Grid for holding data.</summary>
        public IGridView Grid { get { return gridView; } }

        /// <summary>Maximum number of records.</summary>
        public IEditView MaximumNumberRecords { get { return editView2; } }
    }
}
