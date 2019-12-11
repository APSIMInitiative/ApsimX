// -----------------------------------------------------------------------
// <copyright file="DataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using System;


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

        /// <summary>Editable row filter.</summary>
        IEditView RowFilter { get; }

        /// <summary>Filename textbox.</summary>
        IEditView FileName { get; }

        /// <summary>
        /// Invoked when the user changes the filename, either via text input,
        /// or via the file choose dialog.
        /// </summary>
        event EventHandler FileNameChanged;
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
        private EditView rowFilter;
        private VBox vbox1 = null;
        private Table table1 = null;
        private HBox hbox1 = null;

        /// <summary>
        /// Button which can be used to select a new file name, via a file
        /// chooser dialog.
        /// </summary>
        private Button chooseFile = null;

        /// <summary>
        /// Filename textbox.
        /// </summary>
        private EditView fileName;

        /// <summary>
        /// Invoked when the user changes the filename, either via text input,
        /// or via the file choose dialog.
        /// </summary>
        public event EventHandler FileNameChanged;

        /// <summary>Initializes a new instance of the <see cref="DataStoreView" /> class.</summary>
        public DataStoreView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.DataStoreView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            table1 = (Table)builder.GetObject("table1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            mainWidget = vbox1;
            gridView = new GridView(this)
            {
                ReadOnly = true,
                CanGrow = false
            };
            vbox1.PackStart(gridView.MainWidget, true, true, 0);
            vbox1.ReorderChild(hbox1, 2);

            fileName = new EditView(this);
            fileName.Leave += OnFileNameChanged;
            chooseFile = new Button("...");
            chooseFile.Clicked += OnChooseFile;
            HBox fileNameContainer = new HBox();
            fileNameContainer.PackStart(fileName.MainWidget, true, true, 0);
            fileNameContainer.PackStart(chooseFile, false, false, 0);

            dropDownView1 = new DropDownView(this);
            editView1 = new EditView(this);
            rowFilter = new EditView(this);
            table1.Attach(fileNameContainer, 1, 2, 0, 1);
            table1.Attach(dropDownView1.MainWidget, 1, 2, 1, 2);
            table1.Attach(editView1.MainWidget, 1, 2, 2, 3);
            table1.Attach(rowFilter.MainWidget, 1, 2, 3, 4);
            editView2 = new EditView(this);
            hbox1.PackStart(editView2.MainWidget, false, false, 0);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }


        /// <summary>
        /// Does cleanup when the main widget is destroyed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            fileName.Leave -= OnFileNameChanged;
            gridView.Dispose();
            gridView = null;
            dropDownView1.MainWidget.Destroy();
            dropDownView1 = null;
            editView1.MainWidget.Destroy();
            editView1 = null;
            editView2.MainWidget.Destroy();
            editView2 = null;
            rowFilter.MainWidget.Destroy();
            rowFilter = null;
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            chooseFile.Clicked -= OnChooseFile;
            owner = null;
        }

        /// <summary>
        /// Invoked when the user clicks on the chose file '...' button.
        /// Prompts the user to choose a file name via the file chooser dialog,
        /// then signals to the presenter that the file name has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnChooseFile(object sender, EventArgs e)
        {
            string newFileName = AskUserForFileName("Choose a file name", Utility.FileDialog.FileActionType.Save, "SQLite Database (*.db)|*.db|All Files (*.*)|*.*");
            if (!string.IsNullOrEmpty(newFileName))
            {
                FileName.Value = newFileName;
                FileNameChanged?.Invoke(FileName, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked when the user changes the filename by typing into the textbox.
        /// Signals to the presenter that the file name has changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFileNameChanged(object sender, EventArgs e)
        {
            FileNameChanged?.Invoke(FileName, EventArgs.Empty);
        }

        /// <summary>List of all tables.</summary>
        public IDropDownView TableList { get { return dropDownView1; } }

        /// <summary>Editable column filter.</summary>
        public IEditView ColumnFilter { get { return editView1; } }

        /// <summary>Grid for holding data.</summary>
        public IGridView Grid { get { return gridView; } }

        /// <summary>Maximum number of records.</summary>
        public IEditView MaximumNumberRecords { get { return editView2; } }

        /// <summary>Editable row filter.</summary>
        public IEditView RowFilter { get { return rowFilter; } }

        /// <summary>Filename textbox.</summary>
        public IEditView FileName { get { return fileName; } }
    }
}
