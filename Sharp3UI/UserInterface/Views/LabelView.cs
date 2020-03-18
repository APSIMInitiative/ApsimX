
namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;

    /// <summary>A drop down view.</summary>
    public class LabelView : ViewBase, ILabelView
    {
        private Label label;
        
        /// <summary>Constructor</summary>
        public LabelView(ViewBase owner, Label l) : base(owner)
        {
            label = l;
            mainWidget = label;
        }

        /// <summary>Gets or sets the text of the label.</summary>
        public string Value
        {
            get
            {
                return label.Text;
            }
            set
            {
                label.Text = value;
            }
        }
    }
}
