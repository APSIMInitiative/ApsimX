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
        /// <param name="factors">Dictionary mapping factor names to lists of factor values (stored as strings).</param>
        /// <param name="n">Total number of combinations.</param>
        public void Initialise(List<List<Tuple<string, string, string>>> allCombinations)
        {
            if (allCombinations.Count < 1) return;

            Type[] types = new Type[allCombinations[0].Count + 1];
            tree = new TreeView();
            columns = new List<TreeViewColumn>();
            cells = new List<CellRendererText>();            

            types[0] = typeof(string);
            cells.Add(new CellRendererText());
            columns.Add(new TreeViewColumn { Title = "Simulation Name" });
            columns[0].PackStart(cells[0], false);
            columns[0].AddAttribute(cells[0], "text", 0);
            tree.AppendColumn(columns[0]);

            int i = 1;
            // initialise column headers
            foreach (Tuple<string, string, string> factor in allCombinations[0])
            {
                types[i] = typeof(string);                                
                cells.Add(new CellRendererText());
                columns.Add(new TreeViewColumn { Title = factor.Item1 });
                columns[i].PackStart(cells[i], false);
                columns[i].AddAttribute(cells[i], "text", i);

                tree.AppendColumn(columns[i]);
                i++;
            }

            store = new ListStore(types);

            foreach (List<Tuple<string, string, string>> factors in allCombinations)
            {
                List<string> data = new List<string> { factors[0].Item3 };
                foreach(Tuple<string, string, string> factor in factors)
                {
                    data.Add(factor.Item2);
                }
                store.AppendValues(data.ToArray());
            }
            
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;
            tree.Model = store;

            
            
            primaryContainer.Add(tree);            
            primaryContainer.ShowAll();
            tree.ShowAll();
        }
    }
}
