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

        /// <summary>
        /// Hide or show either of the scrollbars
        /// </summary>
        public void SetScrollbarVisible(bool verticalBar, bool visible)
        {
            if (MainWidget != null)
            {
                Box box = (MainWidget as Box);
                if (box.Children.Length > 0)
                {
                    Container container = (Container)box.Children[0];
                    if (verticalBar)
                    {
                        if (container.Children.Length > 0)
                        {
                            Container container2 = (Gtk.Container)container.Children[0];
                            if (container2.Children.Length > 1)
                            {
                                VScrollbar verticalScrollBar = (VScrollbar)container2.Children[1];
                                if (visible)
                                    verticalScrollBar.Show();
                                else
                                    verticalScrollBar.Hide();
                            }
                        }
                        
                    }
                    else
                    {
                        if (container.Children.Length > 1)
                        {
                            HScrollbar horizontalScrollBar = (HScrollbar)container.Children[1];
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
}
