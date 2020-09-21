namespace Utility
{
    using Gtk;
    using System;
    using System.Reflection;

    public static class GtkUtil
    {
        public static void DetachHandlers(this Widget widget)
        {
            PropertyInfo[] signalProperties = new PropertyInfo[]
            {
                widget.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance),
                widget.GetType().GetProperty("BeforeSignals", BindingFlags.NonPublic | BindingFlags.Instance),
            };
            foreach (PropertyInfo pi in signalProperties)
            {
                if (pi != null)
                {
                    System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(widget);
                    if (handlers == null)
                        return;
                    
                    foreach (string eventName in handlers.Keys)
                    {
                        Delegate eventDelegate = (Delegate)handlers[eventName];
                        foreach (Delegate handler in eventDelegate.GetInvocationList())
                            if (handler.Target != null && handler.Target.GetType().Assembly == Assembly.GetExecutingAssembly())
                                Delegate.Remove(eventDelegate, handler);
                    }
                }
            }
        }
    }
}