using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

namespace UserInterface.Views
{
    public class FactorControlView : ViewBase
    {
        public Presenters.FactorControlPresenter Presenter { get; set; }

        /// <summary>
        /// Primary container holding the TreeView and controls vbox.
        /// </summary>
        private HBox primaryContainer;

        /// <summary>
        /// Tree view to layout the data.
        /// </summary>
        private TreeView tree;

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
        private Button btnEnable;

        /// <summary>
        /// Button to disable the selected simulations.
        /// </summary>
        private Button btnDisable;

        /// <summary>
        /// Button to export factor data to a csv file.
        /// </summary>
        private Button btnExportCsv;

        /// <summary>
        /// Button to import factor data from a csv file.
        /// </summary>
        private Button btnImportCsv;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owner"></param>
        public FactorControlView(ViewBase owner) : base(owner)
        {
            primaryContainer = new HBox();
            //Application.Invoke(delegate
            //{                        
                _mainWidget = primaryContainer;
                primaryContainer.ShowAll();
            
            //});
        }

        /// <summary>
        /// Initialises and populates the TreeView.
        /// </summary>
        /// <param name="columnNames">The names of the columns.</param>
        /// <param name="simulations">List of simulations. Each simulation is a tuple comprised of the simulation name, the list of factor levels, and an enabled/disabled flag.</param>
        public void Initialise(List<string> columnNames)
        {
            //primaryContainer = new HBox();
            //_mainWidget = primaryContainer;

            Type[] types = new Type[columnNames.Count];
            tree = new TreeView();
            columns = new List<TreeViewColumn>();
            cells = new List<CellRendererText>();

            // initialise column headers            
            for (int i = 0; i < columnNames.Count; i++)
            {
                types[i] = typeof(string);
                cells.Add(new CellRendererText());
                columns.Add(new TreeViewColumn { Title = columnNames[i] });
                columns[i].PackStart(cells[i], false);
                columns[i].AddAttribute(cells[i], "text", i);

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

            btnEnable = new Button("Enable");            
            btnEnable.Clicked += (sender, e) => { BtnToggle(true); };
            HBox enableButtonContainer = new HBox();
            enableButtonContainer.PackStart(btnEnable, false, false, 0);

            btnDisable = new Button("Disable");
            btnDisable.Clicked += (sender, e) => { BtnToggle(false); };
            HBox disableButtonContainer = new HBox();
            disableButtonContainer.PackStart(btnDisable, false, false, 0);

            btnExportCsv = new Button("Generate CSV");
            btnExportCsv.Clicked += (sender, e) => { Presenter.GenerateCsv(); };
            HBox csvExportButtonContainer = new HBox();
            csvExportButtonContainer.PackStart(btnExportCsv, false, false, 0);

            btnImportCsv = new Button("Import factor information from CSV file");
            btnImportCsv.Clicked += (sender, e) => { Presenter.ImportCsv(AskUserForFileName("Choose a .csv file", "CSV file | *.csv")); };
            HBox csvImportButtonCOntainer = new HBox();
            csvImportButtonCOntainer.PackStart(btnImportCsv, false, false, 0);

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(enableButtonContainer, false, false, 0);
            controlsContainer.PackStart(disableButtonContainer, false, false, 0);
            controlsContainer.PackStart(csvExportButtonContainer, false, false, 0);
            controlsContainer.PackStart(csvImportButtonCOntainer, false, false, 0);


            //(((Presenter.explorerPresenter.GetView().MainWidget as VBox).Children[1] as HPaned).Child2 as ScrolledWindow).HscrollbarPolicy = PolicyType.Always;            
            ScrolledWindow sw = new ScrolledWindow();
            sw.Add(tree);
            sw.HscrollbarPolicy = PolicyType.Automatic;
            sw.VscrollbarPolicy = PolicyType.Automatic;
            //Frame test = new Frame("Factor Control");
            //test.Add(sw);

            Application.Invoke(delegate
            {                
                primaryContainer.PackStart(sw, true, true, 0);
                primaryContainer.PackStart(controlsContainer, false, false, 0);
                
                primaryContainer.ShowAll();
            });
            
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
        /// Event handler for Enable button's click event. 
        /// Passes the names of the currently selected rows and asks it to change their status to enabled.
        /// </summary>
        /// <param name="flag">If true, the selected items will be enabled. If false they will be disabled.</param>
        private void BtnToggle(bool flag)
        {
            Application.Invoke(delegate
            {
                TreePath[] selectedRows = tree.Selection.GetSelectedRows();
                List<string> simNames = new List<string>();
                TreeIter iter;
                foreach (TreePath row in selectedRows)
                {
                    tree.Model.GetIter(out iter, row);
                    simNames.Add((string)tree.Model.GetValue(iter, 0));
                }
                Presenter.ToggleSims(simNames, flag);
            });
        }
    }
}
