using System;
using System.Collections.Generic;
using System.Linq;
using UserInterface.EventArguments;
using Gtk;
using Pango;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public class ExperimentView : ViewBase, IExperimentView
    {
        /// <summary>
        /// Primary container holding the TreeView and controls vbox.
        /// </summary>
        private HBox primaryContainer;

        /// <summary>
        /// Tree view to layout the data.
        /// </summary>
        private Gtk.TreeView tree;

        /// <summary>
        /// List holding the tree columns.
        /// </summary>
        private List<TreeViewColumn> columns;

        /// <summary>
        /// List holding the tree cells.
        /// </summary>
        private List<CellRendererText> cells;

        /// <summary>
        /// List store to hold the data.
        /// </summary>
        private ListStore store;

        /// <summary>
        /// Button to enable the selected simulations.
        /// </summary>
        private Button enableButton;

        /// <summary>
        /// Button to disable the selected simulations.
        /// </summary>
        private Button disableButton;

        /// <summary>
        /// Button to export factor data to a csv file.
        /// </summary>
        private Button exportButton;

        /// <summary>
        /// Button to import factor data from a csv file.
        /// </summary>
        private Button importButton;

        /// <summary>
        /// Button to allow the user to select a maximum number of simulations to display.
        /// </summary>
        private Button changeMaxSimsButton;

        /// <summary>
        /// Label to display the total number of simulations
        /// </summary>
        private Label numSimsLabel;

        /// <summary>
        /// Textbox to allow the user to input a maximum number of simulations to display.
        /// </summary>
        private Entry maxSimsInput;

        /// <summary>
        /// Context menu that appears after the user right clicks on a simulation.
        /// </summary>
        private Menu contextMenu;

        /// <summary>
        /// Context menu option to run the currently selected simulations.
        /// </summary>
        private MenuItem run;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owner"></param>
        public ExperimentView(ViewBase owner) : base(owner)
        {
            primaryContainer = new HBox();          
            _mainWidget = primaryContainer;
            primaryContainer.ShowAll();
        }

        /// <summary>
        /// Invoked when the user wishes to export the current factor information to a .csv file.
        /// </summary>
        public event EventHandler<FileActionArgs> ExportCsv;

        /// <summary>
        /// Invoked when the user wishes to export the current factor information to a .csv file.
        /// </summary>
        public event EventHandler<FileActionArgs> ImportCsv;

        /// <summary>
        /// Invoked when the user wishes to run simulations.
        /// </summary>
        public event EventHandler RunSims;

        /// <summary>
        /// Invoked when the user wishes to enable the selected simulations.
        /// </summary>
        public event EventHandler EnableSims;

        /// <summary>
        /// Invoked when the user wishes to disable the selected simulations.
        /// </summary>
        public event EventHandler DisableSims;

        /// <summary>
        /// Invoked when the user changes the maximum number of simulations to display at once.
        /// </summary>
        public event EventHandler SetMaxSims;

        /// <summary>
        /// Gets the names of the selected simulations.
        /// </summary>
        public List<string> SelectedItems
        {
            get
            {
                TreePath[] selectedRows = tree.Selection.GetSelectedRows();
                List<string> simNames = new List<string>();
                TreeIter iter;
                foreach (TreePath row in selectedRows)
                {
                    tree.Model.GetIter(out iter, row);
                    simNames.Add((string)tree.Model.GetValue(iter, 0));
                }
                return simNames;
            }
        }

        /// <summary>
        /// Gets or sets the max number of sims to display.
        /// </summary>
        public string MaxSimsToDisplay
        {
            get
            {
                return maxSimsInput.Text;
            }
            set
            {
                maxSimsInput.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the value displayed in the number of simulations label.
        /// </summary>
        public string NumSims
        {
            get
            {
                return numSimsLabel.Text;
            }
            set
            {
                numSimsLabel.Text = value + " simulations total.";
            }
        }

        /// <summary>
        /// Initialises and populates the TreeView.
        /// </summary>
        /// <param name="columnNames">The names of the columns.</param>
        /// <param name="simulations">List of simulations. Each simulation is a tuple comprised of the simulation name, the list of factor levels, and an enabled/disabled flag.</param>
        public void Initialise(List<string> columnNames)
        {
            Type[] types = new Type[columnNames.Count];
            tree = new Gtk.TreeView();
            tree.ButtonPressEvent += TreeClicked;
            columns = new List<TreeViewColumn>();
            cells = new List<CellRendererText>();

            // initialise column headers            
            for (int i = 0; i < columnNames.Count; i++)
            {
                types[i] = typeof(string);
                cells.Add(new CellRendererText());
                columns.Add(new TreeViewColumn { Title = columnNames[i], Resizable = true, Sizing = TreeViewColumnSizing.GrowOnly });
                columns[i].PackStart(cells[i], false);
                columns[i].AddAttribute(cells[i], "text", i);
                columns[i].AddNotification("width", ColWidthChange);
                tree.AppendColumn(columns[i]);
            }

            store = new ListStore(types);
            tree.Model = store;
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;
            tree.RulesHint = true;
            string style = "style \"custom-treestyle\"{ GtkTreeView::odd-row-color = \"#ECF2FD\" GtkTreeView::even-row-color = \"#FFFFFF\" GtkTreeView::allow-rules = 1 } widget \"*custom_treeview*\" style \"custom-treestyle\"";
            tree.Name = "custom_treeview";
            Rc.ParseString(style);

            enableButton = new Button("Enable");
            enableButton.Clicked += EnableSims;
            HBox enableButtonContainer = new HBox();
            enableButtonContainer.PackStart(enableButton, true, true, 0);

            disableButton = new Button("Disable");
            disableButton.Clicked += DisableSims;
            HBox disableButtonContainer = new HBox();
            disableButtonContainer.PackStart(disableButton, true, true, 0);

            exportButton = new Button("Generate CSV");
            exportButton.Clicked += OnExportToCsv;
            HBox csvExportButtonContainer = new HBox();
            csvExportButtonContainer.PackStart(exportButton, true, true, 0);

            importButton = new Button("Import factor information from CSV file");
            importButton.Clicked += OnImportCsv;
            HBox csvImportButtonCOntainer = new HBox();
            csvImportButtonCOntainer.PackStart(importButton, true, true, 0);
            
            maxSimsInput = new Entry(Presenters.ExperimentPresenter.DefaultMaxSims.ToString());
            changeMaxSimsButton = new Button("Apply");
            changeMaxSimsButton.Clicked += BtnMaxSims_Click;
            
            HBox maxSimsContainer = new HBox();
            maxSimsContainer.PackStart(maxSimsInput, true, true, 0);
            maxSimsContainer.PackStart(changeMaxSimsButton, false, false, 0);
            
            numSimsLabel = new Label { Xalign = 0f };

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(new Label("Max number of simulations to display:"), false, false, 0);
            controlsContainer.PackStart(maxSimsContainer, false, false, 0);
            controlsContainer.PackStart(new Label(""), false, false, 0);
            controlsContainer.PackStart(enableButtonContainer, false, false, 0);
            controlsContainer.PackStart(disableButtonContainer, false, false, 0);
            controlsContainer.PackStart(csvExportButtonContainer, false, false, 0);
            controlsContainer.PackStart(csvImportButtonCOntainer, false, false, 0);
            controlsContainer.PackEnd(numSimsLabel, false, false, 0);

            ScrolledWindow sw = new ScrolledWindow();
            sw.Add(tree);
            sw.HscrollbarPolicy = PolicyType.Automatic;
            sw.VscrollbarPolicy = PolicyType.Automatic;

            AccelGroup agr = new AccelGroup();
            maxSimsInput.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Return, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            maxSimsInput.Activated += BtnMaxSims_Click;
            Application.Invoke(delegate
            {                
                primaryContainer.PackStart(sw, true, true, 0);
                primaryContainer.PackStart(controlsContainer, false, false, 0);
                
                primaryContainer.ShowAll();
            });

            contextMenu = new Menu();
            run = new MenuItem("Run");
            run.ButtonPressEvent += OnRunSim;
            contextMenu.Add(run);
        }

        /// <summary>
        /// Populates the TreeView with data.
        /// </summary>
        /// <param name="simulations">List of rows. Each row represents a single simulation and is a tuple, made up of a string (simulation name), a list of strings (factor levels) and a boolean (whether the simulation is currently enabled).</param>
        public void Populate(List<Tuple<string, List<string>, bool>> simulations)
        {
            Application.Invoke(delegate
            {
                store.Clear();                
                foreach (Tuple<string, List<string>, bool> sim in simulations)
                {
                    // First cell in the row needs to hold the simulation name
                    List<string> data = new List<string> { sim.Item1 };
                    foreach (string level in sim.Item2)
                    {
                        data.Add(level);
                    }
                    data.Add(sim.Item3.ToString());
                    store.AppendValues(data.ToArray());                                        
                }                
            });            
        }

        /// <summary>
        /// Do cleanup work.
        /// </summary>
        public void Detach()
        {
            exportButton.Clicked -= OnExportToCsv;
            importButton.Clicked -= OnImportCsv;
            disableButton.Clicked -= DisableSims;
            enableButton.Clicked -= EnableSims;
            changeMaxSimsButton.Clicked -= SetMaxSims;
            run.ButtonPressEvent -= OnRunSim;

            store.Dispose();
            contextMenu.Dispose();
            MainWidget.Destroy();
            _owner = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void ColWidthChange(object sender, EventArgs e)
        {
            TreeViewColumn col = sender as TreeViewColumn;
            int index = columns.IndexOf(col);
            Application.Invoke(delegate
            {
                // if something is going wrong with column width, this is probably causing it                
                cells[index].Width = col.Width - 4;
                cells[index].Ellipsize = EllipsizeMode.End;
            });
        }

        /// <summary>
        /// Sets the maximum number of simulations to be displayed in the table.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void BtnMaxSims_Click(object sender, EventArgs e)
        {
            SetMaxSims?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event handler for clicking on the TreeView. 
        /// Shows the context menu (if and only if the click is a right click).
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void TreeClicked(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Button == 3) // right click
            {
                TreePath path;
                tree.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path);

                // By default, Gtk will un-select the selected rows when a normal (non-shift/ctrl) click is registered.
                // Setting e.Retval to true will stop the default Gtk ButtonPress event handler from being called after 
                // we return from this handler, which in turn means that the rows will not be deselected.
                e.RetVal = tree.Selection.GetSelectedRows().Contains(path);
                contextMenu.ShowAll();
                contextMenu.Popup();
            }
        }

        /// <summary>
        /// Event handler for selecting 'run this simulation' from the context menu.
        /// Runs the selected simulations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnRunSim(object sender, ButtonPressEventArgs e)
        {
            contextMenu.HideAll();
            // process everything in the Gtk event queue so the context menu disappears immediately.
            while (GLib.MainContext.Iteration()) ;
            RunSims?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when the user clicks the 'Export to CSV' button.
        /// Asks the user for a filename and generates a .csv file from the factor information.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnExportToCsv(object sender, EventArgs args)
        {
            string fileName = AskUserForFileName("Export to CSV", Utility.FileDialog.FileActionType.Save, "CSV file (*.csv) | *.csv");
            ExportCsv?.Invoke(this, new FileActionArgs { Path = fileName });
        }

        /// <summary>
        /// Invoked when the user clicks the 'Import CSV' button.
        /// Asks the user for a filename and generates a .csv file from the factor information.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnImportCsv(object sender, EventArgs args)
        {
            string fileName = AskUserForFileName("Choose a .csv file", Utility.FileDialog.FileActionType.Open, "CSV File (*.csv) | *.csv");
            ImportCsv?.Invoke(this, new FileActionArgs { Path = fileName });
        }
    }
}
