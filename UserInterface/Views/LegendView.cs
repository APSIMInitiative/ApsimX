using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UserInterface.Views
{
    public delegate void PositionChangedDelegate(string NewText);

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface ILegendView
    {
        event PositionChangedDelegate OnPositionChanged;
        void Populate(string title, string[] values);

        void SetSeriesNames(string[] seriesNames);
        void SetDisabledSeriesNames(string[] seriesNames);
        string[] GetDisabledSeriesNames();
        event EventHandler DisabledSeriesChanged;
        
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class LegendView : UserControl, ILegendView
    {
        private string OriginalText;

        public event PositionChangedDelegate OnPositionChanged;
        public event EventHandler DisabledSeriesChanged;

        /// <summary>
        /// Construtor
        /// </summary>
        public LegendView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populate the view with the specified title.
        /// </summary>
        public void Populate(string title, string[] values)
        {
            LegendPositionCombo.Items.Clear();
            LegendPositionCombo.Items.AddRange(values);
            LegendPositionCombo.Text = title;
        }

        /// <summary>
        /// When the user 'enters' the position combo box, save the current text value for later.
        /// </summary>
        private void OnTitleTextBoxEnter(object sender, EventArgs e)
        {
            OriginalText = LegendPositionCombo.Text;
        }

        /// <summary>
        /// When the user changes the combo box check to see if the text has changed. 
        /// If so then invoke the 'OnPositionChanged' event so that the presenter can pick it up.
        /// </summary>
        private void OnPositionComboChanged(object sender, EventArgs e)
        {
            if (OriginalText == null)
                OriginalText = LegendPositionCombo.Text;
            if (LegendPositionCombo.Text != OriginalText && OnPositionChanged != null)
            {
                OriginalText = LegendPositionCombo.Text;
                OnPositionChanged.Invoke(LegendPositionCombo.Text);
            }
        }

        /// <summary>Sets the series names.</summary>
        /// <param name="seriesNames">The series names.</param>
        public void SetSeriesNames(string[] seriesNames)
        {
            listView1.Items.Clear();
            foreach (string seriesName in seriesNames)
            {
                ListViewItem item = new ListViewItem(seriesName);
                item.Checked = true;
                listView1.Items.Add(item);
            }
        }

        /// <summary>Sets the disabled series names.</summary>
        /// <param name="seriesNames">The series names.</param>
        public void SetDisabledSeriesNames(string[] seriesNames)
        {
            foreach (string seriesName in seriesNames)
            {
                ListViewItem item = IndexOf(seriesName);
                if (item != null)
                    item.Checked = false;
            }
        }

        /// <summary>Returns the index of an item.</summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public ListViewItem IndexOf(string text)
        {
            foreach (ListViewItem item in listView1.Items)
                if (item.Text == text)
                    return item;
            return null;
        }

        /// <summary>Gets the disabled series names.</summary>
        /// <returns></returns>
        public string[] GetDisabledSeriesNames()
        {
            List<string> disabledSeries = new List<string>();
            foreach (ListViewItem item in listView1.Items)
                if (!item.Checked)
                    disabledSeries.Add(item.Text);
            return disabledSeries.ToArray();
        }

        /// <summary>Called when user checks an item.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ItemCheckedEventArgs"/> instance containing the event data.</param>
        private void OnItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (DisabledSeriesChanged != null)
                DisabledSeriesChanged.Invoke(this, new EventArgs());
        }
        
    }
}
