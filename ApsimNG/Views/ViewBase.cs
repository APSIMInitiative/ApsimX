using System;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Glade;
using Gtk;
using UserInterface.Views;


namespace UserInterface
{
     public class ViewBase
    {
        static string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        protected ViewBase _owner = null;
        protected Widget _mainWidget = null;
        public ViewBase Owner { get { return _owner; } }
        public Widget MainWidget { get { return _mainWidget; } }
        public ViewBase(ViewBase owner) { _owner = owner; }

        protected Gdk.Window mainWindow { get { return MainWidget == null ? null : MainWidget.Toplevel.GdkWindow; } }
        private bool waiting = false;

        protected bool hasResource(string name)
        {
            return resources.Contains(name);
        }
        public bool WaitCursor
        {
            get
            {
                return waiting;
            }
            set
            {
                if (mainWindow != null)
                {
                    if (value == true)
                        mainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                    else
                        mainWindow.Cursor = null;
                    while (Gtk.Application.EventsPending())
                        Gtk.Application.RunIteration();
                    waiting = value;
                }
            }
        }
    }
}