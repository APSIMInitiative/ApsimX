using Gtk;

namespace UserInterface.Views
{
    /// <summary>A container view.</summary>
    public class ContainerView : ViewBase
    {
        private Container container;

        /// <summary>Constructor</summary>
        public ContainerView() { }

        ///// <summary>Constructor</summary>
        public ContainerView(ViewBase owner) : base(owner)
        {
            container = new Box(Orientation.Vertical, 0) as Container;
            Initialise(owner, container);
        }

        /// <summary>Constructor</summary>
        public ContainerView(ViewBase owner, Container e) : base(owner)
        {
            Initialise(owner, e);
        }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            container = (Container)gtkControl;
            this.mainWidget = container;
        }

        public void Add(Widget child)
        {
            if (container.Children.Length > 0)
                container.Remove(container.Children[0]);
            if (container is Box box)
                box.PackStart(child, true, true, 0);
            else
                container.Add(child);
            container.ShowAll();
        }

        /// <summary>
        /// Hide the container and all of its children.
        /// </summary>
        public void Hide()
        {
            container.Hide();
        }

        /// <summary>
        /// Show the container and all of its children.
        /// </summary>
        public void Show()
        {
            container.ShowAll();
        }

        /// <summary>
        /// Hide or show either of the scrollbars
        /// </summary>
        public void SetScrollbarVisible(bool verticalBar, bool visible)
        {
            if (MainWidget != null)
            {
                Container cont = container;
                if (MainWidget is Box box && box.Children.Length > 0)
                    cont = (Container)box.Children[0];
                
                if (verticalBar)
                {
                    if (cont.Children.Length > 0)
                    {
                        Container cont2 = (Gtk.Container)cont.Children[0];
                        if (cont2.Children.Length > 1)
                        {
                            VScrollbar verticalScrollBar = (VScrollbar)cont2.Children[1];
                            if (visible)
                                verticalScrollBar.Show();
                            else
                                verticalScrollBar.Hide();
                        }
                    }
                    
                }
                else
                {
                    if (cont.Children.Length > 1)
                    {
                        HScrollbar horizontalScrollBar = (HScrollbar)cont.Children[1];
                        if (visible)
                            horizontalScrollBar.Show();
                        else
                            horizontalScrollBar.Hide();
                    }
                }
            }

            
        }
    }
}
