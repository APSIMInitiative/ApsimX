using System;
using System.Windows.Forms;

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
        
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class LegendView : UserControl, ILegendView
    {
        private string OriginalText;

        public event PositionChangedDelegate OnPositionChanged;

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
    }
}
