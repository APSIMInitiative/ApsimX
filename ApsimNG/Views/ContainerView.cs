using Gtk;

namespace UserInterface.Views
{
    /// <summary>A container view.</summary>
    public class ContainerView : ViewBase
    {
        /// <summary>Constructor</summary>
        public ContainerView() { }

        ///// <summary>Constructor</summary>
        public ContainerView(ViewBase owner) : base(owner)
        {
            Widget = new Box(Orientation.Vertical, 0) as Container;
            Initialise(owner, Widget);
        }

        public Container Widget { get; set; }

        /// <summary>Constructor</summary>
        public ContainerView(ViewBase owner, Container e) : base(owner)
        {
            Initialise(owner, e);
        }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            Widget = (Container)gtkControl;
            this.mainWidget = Widget;
        }

        /// <summary>
        /// Hide the container and all of its children.
        /// </summary>
        public void Hide()
        {
            Widget.Hide();
        }

        /// <summary>
        /// Show the container and all of its children.
        /// </summary>
        public void Show()
        {
            Widget.ShowAll();
        }
   }
}
