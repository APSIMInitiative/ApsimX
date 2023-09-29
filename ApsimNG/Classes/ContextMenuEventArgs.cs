using System;
using Gtk;

namespace UserInterface.Views
{
    // https://daveaglick.com/posts/right-click-context-menus-in-gtksharp

    public class ContextMenuEventArgs : EventArgs
    {
        private Widget widget;
        public Widget Widget { get { return widget; } }

        private double x;
        public double X { get { return x; } }
        private double y;
        public double Y { get { return y; } }

        public ContextMenuEventArgs(Widget widget, double X, double Y)
        {
            this.widget = widget;
            this.x = X;
            this.y = Y;
        }
    }
}