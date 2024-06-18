using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Gtk;
using UserInterface.Views;

namespace Utility
{
    public static class GtkUtilities
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
                    if (child != null)
                        child.DetachAllHandlers();
        }

        /// <summary>
        /// Detach all event handlers defined in ApsimNG from the widget.
        /// This procedure uses reflection to gain access to non-public properties and fields.
        /// The procedure is not entirely safe, in that it makes assumptions about the internal handling of event
        /// signalling in Gtk#. This may break in future versions if the Gtk# internals change. This sort of
        /// breakage has occurred with an earlier version of this routine.
        /// A "breakage" probably won't be immediately apparent, but may lead to memory leaks, as the main
        /// reason for having this routine is to remove references that can prevent garbage collection.
        /// </summary>
        /// <param name="widget">The widget.</param>
        public static void DetachHandlers(this Widget widget)
        {
            PropertyInfo signals = typeof(GLib.Object).GetProperty("Signals", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo afterHandler = typeof(GLib.Signal).GetField("after_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo beforeHandler = typeof(GLib.Signal).GetField("before_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (signals != null && afterHandler != null && beforeHandler != null)
            {
                Dictionary<string, GLib.Signal> widgetSignals = (Dictionary<string, GLib.Signal>)signals.GetValue(widget);
                foreach (KeyValuePair<string, GLib.Signal> signal in widgetSignals)
                {
                    if (signal.Key != "destroy")
                    {
                        GLib.Signal signalVal = signal.Value;
                        Delegate afterDel = (Delegate)afterHandler.GetValue(signalVal);
                        if (afterDel != null)
                            widget.RemoveSignalHandler(signal.Key, afterDel);
                        Delegate beforeDel = (Delegate)beforeHandler.GetValue(signalVal);
                        if (beforeDel != null)
                            widget.RemoveSignalHandler(signal.Key, beforeDel);
                    }
                }
            }
        }

        /// <summary>
        /// Detach a specific signal from a widget.
        /// Normally it's better to use the "-=" operator to do this, but it may be that we
        /// don't actually know the method for the right side of the operator. This uses
        /// reflection to find that method and detach it. As explained in the DetachHandlers 
        /// method above, this routine may break in future versions of Gtk#.
        /// </summary>
        /// <param name="widget">The widget</param>
        /// <param name="signalName">Name of the signal</param>
        public static bool DetachHandler(this Widget widget, string signalName)
        {
            bool result = false;
            PropertyInfo signals = typeof(GLib.Object).GetProperty("Signals", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo afterHandler = typeof(GLib.Signal).GetField("after_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo beforeHandler = typeof(GLib.Signal).GetField("before_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (signals != null && afterHandler != null && beforeHandler != null)
            {
                Dictionary<string, GLib.Signal> widgetSignals = (Dictionary<string, GLib.Signal>)signals.GetValue(widget);
                GLib.Signal signalVal;
                if (widgetSignals.TryGetValue(signalName, out signalVal))
                {
                    Delegate afterDel = (Delegate)afterHandler.GetValue(signalVal);
                    if (afterDel != null)
                    {
                        widget.RemoveSignalHandler(signalName, afterDel);
                        result = true;
                    }
                    Delegate beforeDel = (Delegate)beforeHandler.GetValue(signalVal);
                    if (beforeDel != null)
                    {
                        widget.RemoveSignalHandler(signalName, beforeDel);
                        result = true;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Remove all items from a Menu, ensuring that their handlers are detached 
        /// </summary>
        /// <param name="menu">The menu</param>
        public static void Clear(this Menu menu)
        {
            foreach (Widget w in menu)
            {
                // We're being cautious here. In CodeView.cs, the popup menu is partially
                // constructed by Gtk.TextView, using who-knows-what magic. "Their" menu
                // items aren't using the activate signal (I don't know how they work),
                // but the activate handler is the link we need to break to all garbage collection.
                // We get nasty problems if we try to dispose of those TextView items
                bool canDispose = w.DetachHandler("activate") || w is SeparatorMenuItem;
                menu.Remove(w);
                if (canDispose)
                    w.Dispose();
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

        /// <summary>
        /// Gets the location of the widget in regards to the graphview
        /// </summary>
        public static System.Drawing.Point GetPositionOfWidgetRelativeToAnotherWidget(Widget widget, Widget otherWidget)
        {
            System.Drawing.Point firstPos = GtkUtilities.GetPositionOfWidget(widget);
            System.Drawing.Point secondPos = GtkUtilities.GetPositionOfWidget(otherWidget);
            return new System.Drawing.Point(firstPos.X - secondPos.X, firstPos.Y - secondPos.Y);
        }

        /// <summary>
        /// Gets the location (in screen coordinates) of the widget.
        /// </summary>
        public static System.Drawing.Point GetPositionOfWidget(Widget widget)
        {
            if (widget == null)
                return new System.Drawing.Point(0, 0);

            // Get the location of the cursor. This rectangle's x and y properties will be
            // the current line and column number.
            Gdk.Rectangle location = widget.Allocation;

            // Now, convert these coordinates to be relative to the GtkWindow's origin.
            widget.TranslateCoordinates(widget.Toplevel, location.X, location.Y, out int windowX, out int windowY);

            Widget win = widget;
            while (win.Parent != null)
                win = win.Parent;

            win.Window.GetOrigin(out int frameX, out int frameY);

            return new System.Drawing.Point(frameX + windowX, frameY + windowY);
        }

        /// <summary>
        /// Returns a rectangle that defines in screen cordinates the edges of the right hand view where content is loaded
        /// This will only work if the explorer view has been fully loaded.
        /// Left side is the edge of the tree view
        /// Right side is the edge of the window
        /// Bottom is the top of the status window
        /// Top is the bottom of the menu bar
        /// </summary>
        public static System.Drawing.Rectangle GetBorderOfRightHandView(ViewBase view)
        {
            ViewBase mainView = view;
            ViewBase explorerView = null;
            while (mainView as MainView == null)
            {
                if (mainView == null) //return a box if this could not compute correctly.
                    return new Rectangle(0, 0, 100, 100);
                else
                    mainView = mainView.Owner;

                if (mainView as ExplorerView != null)
                    explorerView = mainView;
            }          

            if (explorerView == null)
                explorerView = mainView;

            int top = GtkUtilities.GetPositionOfWidget(view.MainWidget).Y;
            int bottom = (mainView as MainView).StatusPanelPosition;
            int left = GtkUtilities.GetPositionOfWidget(view.MainWidget).X;
            int right = GtkUtilities.GetPositionOfWidget(explorerView.MainWidget).X + explorerView.MainWidget.AllocatedWidth;

            int width = right - left;
            int height = bottom - top;

            Rectangle bounds = new Rectangle(left, top, width, height);
            return bounds;
        }
    }
}