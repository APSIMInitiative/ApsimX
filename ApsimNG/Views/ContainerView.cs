using Gtk;

namespace UserInterface.Views
{
    /// <summary>A container view.</summary>
    public class ContainerView : ViewBase
    {
        HBox container;

        /// <summary>Constructor</summary>
        public ContainerView() { }

        ///// <summary>Constructor</summary>
        //public ContainerView(ViewBase owner) : base(owner)
        //{
        //    Initialise(owner, new Container());
        //}

        /// <summary>Constructor</summary>
        public ContainerView(ViewBase owner, Container e) : base(owner)
        {
            Initialise(owner, e);
        }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            container = (HBox)gtkControl;
        }

        public void Add(Widget child)
        {
            if (container.Children.Length > 0)
                container.Remove(container.Children[0]);
            container.PackStart(child, true, true, 10);
            container.ShowAll();
        }
    }
}
