namespace Utility
{
    using Gtk;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class GtkUtil
    {
        /// <summary>
        /// Detaches all event handlers on the widget and all descendants.
        /// </summary>
        /// <param name="widget">The widget.</param>
        public static void DetachAllHandlers(this Widget widget)
        {
            widget.DetachHandlers();
            if (widget is Container container)
                foreach (Widget child in container.Children)
                    child.DetachAllHandlers();
        }

        /// <summary>
        /// Detach all event handlers defined in ApsimNG from the widget.
        /// </summary>
        /// <param name="widget">The widget.</param>
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

        /// <summary>
        /// Get child widget at the specified row and column.
        /// </summary>
        /// <param name="table">A table</param>
        /// <param name="row">Row of the widget (top-attach).</param>
        /// <param name="col">Column of the widget (left-attach).</param>
        public static Widget GetChild(this Table table, uint row, uint col)
        {
            foreach (Widget child in table.Children)
            {
                object topAttach = table.ChildGetProperty(child, "top-attach").Val;
                object leftAttach = table.ChildGetProperty(child, "left-attach").Val;
                if (topAttach.GetType() == typeof(uint) && leftAttach.GetType() == typeof(uint))
                {
                    uint rowIndex = (uint)topAttach;
                    uint colIndex = (uint)leftAttach;

                    if (rowIndex == row && colIndex == col)
                        return child;
                }
            }

            return null;
        }

        public static IEnumerable<Widget> Descendants(this Widget widget)
        {
            List<Widget> descendants = new List<Widget>();
            if (widget is Container container)
            {
                foreach (Widget child in container.Children)
                {
                    descendants.Add(child);
                    descendants.AddRange(child.Descendants());
                }
            }
            return descendants;
        }

        public static IEnumerable<Widget> Ancestors(this Widget widget)
        {
            Widget parent = widget?.Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }
    }
}