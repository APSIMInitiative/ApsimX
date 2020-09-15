namespace Utility
{
    using Gtk;
    using System;
    using System.Reflection;

    public static class GtkUtil
    {
        public static void DetachHandlers(this Widget widget)
        {
            PropertyInfo pi = widget.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi != null)
            {
                System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(widget);
                if (handlers != null && handlers.ContainsKey("activate"))
                {
                    EventHandler handler = (EventHandler)handlers["activate"];
                    (widget as MenuItem).Activated -= handler;
                }
            }
        }
    }
}