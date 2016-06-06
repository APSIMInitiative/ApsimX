// -----------------------------------------------------------------------
// <copyright file="DataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using Glade;


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
    public class DataStoreView : ViewBase, IDataStoreView
    {
        /// <summary>
        /// An output grid view.
        /// </summary>
        private GridView gridView;
        private EditView editView1;
        private DropDownView dropDownView1;
        private EditView editView2;

        [Widget]
        private VBox vbox1;
        [Widget]
        private Table table1;
        [Widget]
        private HBox hbox1;

        /// <summary>Initializes a new instance of the <see cref="DataStoreView" /> class.</summary>
        public DataStoreView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.DataStoreView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;
            gridView = new GridView(this);
            vbox1.PackStart(gridView.MainWidget, true, true, 0);
            vbox1.ReorderChild(hbox1, 2);
            dropDownView1 = new DropDownView(this);
            editView1 = new EditView(this);
            table1.Attach(dropDownView1.MainWidget, 1, 2, 0, 1);
            table1.Attach(editView1.MainWidget, 1, 2, 1, 2);
            editView2 = new EditView(this);
            hbox1.PackStart(editView2.MainWidget, false, false, 0);
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
