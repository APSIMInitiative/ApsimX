using Gtk;

namespace UserInterface.Views
{

    /// <summary>A drop down view.</summary>
    public class LabelView : ViewBase, ILabelView
    {
        private Label label;

        /// <summary>Constructor</summary>
        public LabelView() : base() { }
        
        /// <summary>Constructor</summary>
        public LabelView(ViewBase owner, Label l) : base(owner)
        {
            Initialise(owner, l);
        }

        /// <summary>Text of the label.</summary>
        public string Text
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

        /// <summary>Is the label visible?</summary>
        public bool Visible { get { return label.Visible; } set { label.Visible = value; } }

        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            label = (Label)gtkControl;
            mainWidget = label;
        }
    }

    /// <summary>An interface for a label.</summary>
    public interface ILabelView
    {
        /// <summary>Gets or sets the text of the label.</summary>
        string Text { get; set; }
    }
}
