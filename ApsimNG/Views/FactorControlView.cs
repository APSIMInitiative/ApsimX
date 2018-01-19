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
        /// Constructor
        /// </summary>
        /// <param name="owner"></param>
        public FactorControlView(ViewBase owner) : base(owner)
        {
            primaryContainer = new HBox();            

            _mainWidget = primaryContainer;
            primaryContainer.ShowAll();
        }

        /// <summary>
        /// Initialises and populates the TreeView.
        /// </summary>
        /// <param name="columnNames">The names of the columns.</param>
        /// <param name="simulations">List of simulations. Each simulation is a tuple comprised of the simulation name, the list of factor levels, and an enabled/disabled flag.</param>
        public void Initialise(List<string> columnNames, List<Tuple<string, List<string>, bool>> simulations)
        {
            //primaryContainer = new HBox();
            //_mainWidget = primaryContainer;

            if (simulations.Count < 1) return;

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
            
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;
            tree.Model = store;

            btnEnable = new Button("Enable");
            btnEnable.Clicked += BtnEnable_Click;
            btnDisable = new Button("Disable");
            btnDisable.Clicked += BtnDisable_Click;

            

            HBox enableButtonContainer = new HBox();
            enableButtonContainer.PackStart(btnEnable, false, false, 0);

            HBox disableButtonContainer = new HBox();
            disableButtonContainer.PackStart(btnDisable, false, false, 0);

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(enableButtonContainer, false, false, 0);
            controlsContainer.PackStart(disableButtonContainer, false, false, 0);







            primaryContainer.PackStart(tree, false, true, 0);
            primaryContainer.PackStart(controlsContainer, false, false, 0);
            primaryContainer.ShowAll();
        }

        /// <summary>
        /// Event handler for Enable button's click event. 
        /// Passes the names of the currently selected rows and asks it to change their status to enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnEnable_Click(object sender, EventArgs e)
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();
            List<string> simNames = new List<string>();
            TreeIter iter;
            foreach (TreePath row in selectedRows)
            {
                tree.Model.GetIter(out iter, row);
                simNames.Add((string)tree.Model.GetValue(iter, 0));
            }
            Presenter.ToggleSims(simNames, true);
        }

        private void BtnDisable_Click(object sender, EventArgs e)
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();
            List<string> simNames = new List<string>();
            TreeIter iter;
            foreach (TreePath row in selectedRows)
            {
                tree.Model.GetIter(out iter, row);
                simNames.Add((string)tree.Model.GetValue(iter, 0));
            }
            Presenter.ToggleSims(simNames, false);
        }
    }
}
