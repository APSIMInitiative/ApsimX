// -----------------------------------------------------------------------
// <copyright file="DataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;


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

        private VBox vbox1 = null;
        private Table table1 = null;
        private HBox hbox1 = null;

        /// <summary>Initializes a new instance of the <see cref="DataStoreView" /> class.</summary>
        public DataStoreView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.DataStoreView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            table1 = (Table)builder.GetObject("table1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            _mainWidget = vbox1;
            gridView = new GridView(this)
            {
                ReadOnly = true,
                CanGrow = false
            };
            vbox1.PackStart(gridView.MainWidget, true, true, 0);
            vbox1.ReorderChild(hbox1, 2);
            dropDownView1 = new DropDownView(this);
            editView1 = new EditView(this);
            table1.Attach(dropDownView1.MainWidget, 1, 2, 0, 1);
            table1.Attach(editView1.MainWidget, 1, 2, 1, 2);
            editView2 = new EditView(this);
            hbox1.PackStart(editView2.MainWidget, false, false, 0);
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }


        /// <summary>
        /// Does cleanup when the main widget is destroyed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            gridView.Dispose();
            gridView = null;
            dropDownView1.MainWidget.Destroy();
            dropDownView1 = null;
            editView1.MainWidget.Destroy();
            editView1 = null;
            editView2.MainWidget.Destroy();
            editView2 = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
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
