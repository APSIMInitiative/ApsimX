using System;
using Gtk;
using Gdk;
using GLib;

namespace UserInterface.Views
{

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
            try
            {
                args.RetVal = true;
                RaiseContextMenuEvent(args, (Widget)o, -1, -1);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        [GLib.ConnectBefore]
        private void Widget_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            try
            {
                if (args.Event.Button == 3 && args.Event.Type == EventType.ButtonPress)
                {
                    args.RetVal = true;
                    //Console.WriteLine("e = " + args.Event.X + ",", args.Event.Y);

                    RaiseContextMenuEvent(args, o as Widget, args.Event.X , args.Event.Y);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        private bool propagating = false;   //Prevent reentry

        private void RaiseContextMenuEvent(GLib.SignalArgs signalArgs, Widget widget, double x, double y)
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
                ContextMenuEventArgs args = new ContextMenuEventArgs(widget, x, y);
                if (ContextMenu != null)
                {
                    ContextMenu.Invoke(this, args);
                }
            }
        }
    }
}