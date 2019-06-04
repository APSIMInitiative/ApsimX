using System;
using Gtk;
using Gdk;
using GLib;

namespace UserInterface.Views
{
    // https://daveaglick.com/posts/right-click-context-menus-in-gtksharp

    public class ContextMenuEventArgs : EventArgs
    {
        private Widget widget;
        public Widget Widget { get { return widget; } }

        private bool rightClick;
        public bool RightClick { get { return rightClick; } }

        public ContextMenuEventArgs(Widget widget, bool rightClick)
        {
            this.widget = widget;
            this.rightClick = rightClick;
        }
    }

    public class ContextMenuHelper
    {
        public event EventHandler<ContextMenuEventArgs> ContextMenu;

        public ContextMenuHelper()
        { }

        public ContextMenuHelper(Widget widget)
        {
            AttachToWidget(widget);
        }

        public ContextMenuHelper(Widget widget, EventHandler<ContextMenuEventArgs> handler)
        {
            AttachToWidget(widget);
            ContextMenu += handler;
        }

        public void AttachToWidget(Widget widget)
        {
            widget.PopupMenu += Widget_PopupMenu;
            widget.ButtonPressEvent += Widget_ButtonPressEvent;
        }

        public void DetachFromWidget(Widget widget)
        {
            widget.PopupMenu -= Widget_PopupMenu;
            widget.ButtonPressEvent -= Widget_ButtonPressEvent;
        }

        [GLib.ConnectBefore]
        private void Widget_PopupMenu(object o, PopupMenuArgs args)
        {
            args.RetVal = true;
            RaiseContextMenuEvent(args, (Widget)o, false);
        }

        [GLib.ConnectBefore]
        private void Widget_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3 && args.Event.Type == EventType.ButtonPress)
            {
                args.RetVal = true;
                RaiseContextMenuEvent(args, (Widget)o, true);
            }
        }

        private bool propagating = false;   //Prevent reentry

        private void RaiseContextMenuEvent(GLib.SignalArgs signalArgs, Widget widget, bool rightClick)
        {
            if (!propagating)
            {
                //Propagate the event
                Event evnt = Gtk.Global.CurrentEvent;
                propagating = true;
                Gtk.Global.PropagateEvent(widget, evnt);
                propagating = false;
                signalArgs.RetVal = true;     //The widget already processed the event in the propagation

                //Raise the context menu event
                ContextMenuEventArgs args = new ContextMenuEventArgs(widget, rightClick);
                if (ContextMenu != null)
                {
                    ContextMenu.Invoke(this, args);
                }
            }
        }
    }
}
