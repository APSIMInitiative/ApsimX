using Gtk;

namespace UserInterface.Views
{
    /// <summary>A container view.</summary>
    public class ContainerView : ViewBase
    {
        Container container;

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
            container = (Container)gtkControl;
        }

        public void Add(Widget child)
        {
            container.Add(child);
            container.ShowAll();
        }
    }
}
