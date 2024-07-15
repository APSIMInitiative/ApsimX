using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace UserInterface.Views
{
    public delegate void PositionChangedDelegate(string NewText);


    /// <summary>
    /// A view which allows the user to customise a graph legend.
    /// </summary>
    public class LegendView : ViewBase, ILegendView
    {
        public event EventHandler DisabledSeriesChanged;
        public event EventHandler LegendInsideGraphChanged;

        private Box hbox1 = null;
        private Gtk.TreeView listview = null;

        private ListStore listModel = new ListStore(typeof(Boolean), typeof(string));
        private CellRendererText listRender = new CellRendererText();
        private CellRendererToggle listToggle = new CellRendererToggle();

        private CheckButton chkLegendInsideGraph;

        public IDropDownView OrientationDropDown { get; private set; }
        public IDropDownView PositionDropDown { get; private set; }

        /// <summary>
        /// Construtor
        /// </summary>
        public LegendView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.LegendView.glade");
            hbox1 = (Box)builder.GetObject("hbox1");
            ComboBox combobox1 = (ComboBox)builder.GetObject("combobox1");
            ComboBox orientationCombo = (ComboBox)builder.GetObject("combobox2");
            listview = (Gtk.TreeView)builder.GetObject("listview");
            mainWidget = hbox1;

            OrientationDropDown = new DropDownView(this, orientationCombo);
            PositionDropDown = new DropDownView(this, combobox1);

            chkLegendInsideGraph = (CheckButton)builder.GetObject("chkLegendInsideGraph");
            chkLegendInsideGraph.Toggled += OnToggleLegendInsideGraph;

            listview.Model = listModel;
            TreeViewColumn column = new TreeViewColumn();
            column.Title = "Series name";
            column.PackStart(listToggle, false);
            listRender.Editable = false;
            column.PackStart(listRender, true);
            column.SetAttributes(listToggle, "active", 0);
            column.SetAttributes(listRender, "text", 1);
            listview.AppendColumn(column);
            listToggle.Activatable = true;
            listToggle.Toggled += OnItemChecked;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void OnToggleLegendInsideGraph(object sender, EventArgs e)
        {
            try
            {
                LegendInsideGraphChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                chkLegendInsideGraph.Toggled -= OnToggleLegendInsideGraph;
                listToggle.Toggled -= OnItemChecked;
                listModel.Dispose();
                listRender.Dispose();
                listToggle.Dispose();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Returns whether or not the check button to show the legend inside the graph is checked.
        /// </summary>
        public bool LegendInsideGraph
        {
            get => chkLegendInsideGraph.Active;
            set => chkLegendInsideGraph.Active = value;
        }

        /// <summary>Sets the series names.</summary>
        /// <param name="seriesNames">The series names.</param>
        public void SetSeriesNames(string[] seriesNames)
        {
            listModel.Clear();
            foreach (string seriesName in seriesNames)
                listModel.AppendValues(true, seriesName);
        }

  
        /// <summary>Sets the disabled series names.</summary>
        /// <param name="seriesNames">The series names.</param>
        public void SetDisabledSeriesNames(string[] seriesNames)
        {
            TreeIter iter;
            if (listModel.GetIterFirst(out iter))
            {
                do
                {
                    string entry = (string)listModel.GetValue(iter, 1);
                    if (Array.IndexOf(seriesNames, entry) >= 0)
                        listModel.SetValue(iter, 0, false);
                } while (listModel.IterNext(ref iter));
            }
        }

        /// <summary>Gets the disabled series names.</summary>
        /// <returns></returns>
        public string[] GetDisabledSeriesNames()
        {
            List<string> disabledSeries = new List<string>();
            foreach (object[] row in listModel)
                if ((bool)row[0] == false)
                    disabledSeries.Add((string)row[1]);
            return disabledSeries.ToArray();
        }

        /// <summary>Called when user checks an item.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments> instance containing the event data.</param>
        private void OnItemChecked(object sender, ToggledArgs e)
        {
            try
            {
                TreeIter iter;

                if (listModel.GetIter(out iter, new TreePath(e.Path)))
                {
                    bool old = (bool)listModel.GetValue(iter, 0);
                    listModel.SetValue(iter, 0, !old);
                }
                if (DisabledSeriesChanged != null)
                    DisabledSeriesChanged.Invoke(this, new EventArgs());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>
    /// Describes an interface for a legend view.
    /// </summary>
    interface ILegendView
    {
        bool LegendInsideGraph { get; set; }
        IDropDownView OrientationDropDown { get; }
        IDropDownView PositionDropDown { get; }

        void SetSeriesNames(string[] seriesNames);
        void SetDisabledSeriesNames(string[] seriesNames);
        string[] GetDisabledSeriesNames();

        event EventHandler DisabledSeriesChanged;
        event EventHandler LegendInsideGraphChanged;
    }
}
