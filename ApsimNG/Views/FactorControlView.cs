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

        public void Initialise(List<string> experimentNames, List<string> factorNames)
        {
            
            Type[] types = new Type[factorNames.Count + 1];
            tree = new TreeView();
            columns = new List<TreeViewColumn>();
            cells = new List<CellRendererText>();
            List<string> data = new List<string>();
            
            for (int i = 0; i < factorNames.Count + 1; i++)
            {
                types[i] = typeof(string);
                data.Add(i == 0 ? experimentNames[0] : i % 2 == 0 ? "Active" : "Inactive");
                string header = i == 0 ? "Simulation Name" : factorNames[i - 1];

                cells.Add(new CellRendererText());

                columns.Add(new TreeViewColumn { Title = header });
                columns[i].PackStart(cells[i], false);
                columns[i].AddAttribute(cells[i], "text", i);

                tree.AppendColumn(columns[i]);
            }
            
            
            store = new ListStore(types);
            store.AppendValues(data.ToArray());
            data.ForEach(x => x = x == "Active" ? "Inactive" : x == "Inactive" ? "Active" : factorNames[0]);
            store.AppendValues(data);
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
