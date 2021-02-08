using System;
using Gtk;
using Gdk;
using Cairo;

#if NETCOREAPP
using CellEditable = Gtk.ICellEditable;
#endif

namespace UserInterface.Classes
{
    /// <summary>
    /// This is an attempt to extend the default CellRendererComob widget to allow
    /// a drop-down arrow to be visible at all times, rather than just when editing.
    /// </summary>
    public class CellRendererDropDown : CellRendererCombo
    {
#if NETFRAMEWORK
        /// <summary>
        /// Render the cell in the window.
        /// </summary>
        /// <param name="window">The owning window.</param>
        /// <param name="widget">The widget.</param>
        /// <param name="background_area">Background area.</param>
        /// <param name="cell_area">The cell area.</param>
        /// <param name="expose_area">Expose the area.</param>
        /// <param name="flags">Render flags.</param>
        protected override void Render(Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
        {
            base.Render(window, widget, background_area, cell_area, expose_area, flags);
            Gtk.Style.PaintArrow(widget.Style, window, StateType.Normal, ShadowType.Out, cell_area, widget, string.Empty, ArrowType.Down, true, Math.Max(cell_area.X, cell_area.X + cell_area.Width - 20), cell_area.Y, 20, cell_area.Height);
        }
#else
        /// <summary>
        /// Render the cell in the window.
        /// </summary>
        /// <param name="cr">The drawing context.</param>
        /// <param name="widget">The widget.</param>
        /// <param name="background_area">Background area.</param>
        /// <param name="cell_area">The cell area.</param>
        /// <param name="flags">Render flags.</param>
        protected override void OnRender(Context cr, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
        {
            base.OnRender(cr, widget, background_area, cell_area, flags);
            //tbi
            //Gtk.Style.PaintArrow(widget.Style, window, StateType.Normal, ShadowType.Out, cell_area, widget, string.Empty, ArrowType.Down, true, Math.Max(cell_area.X, cell_area.X + cell_area.Width - 20), cell_area.Y, 20, cell_area.Height);
        }
#endif

        /// <summary>
        /// Called when editing is started. Traps the EditingDone event.
        /// </summary>
        /// <param name="editable">Cell which is to be edited.</param>
        /// <param name="path">Widget-dependent string representation of the event location; e.g. for GtkTreeView, a string representation of GtkTreePath</param>
        protected override void OnEditingStarted(CellEditable editable, string path)
        {
            base.OnEditingStarted(editable, path);
            editable.EditingDone += EditableEditingDone;
        }

        /// <summary>
        /// Called when editing has finished. Give focus back to the treeview.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void EditableEditingDone(object sender, EventArgs e)
        {
            if (sender is CellEditable)
            {
                (sender as CellEditable).EditingDone -= EditableEditingDone;
                if (sender is Widget && (sender as Widget).Parent is Gtk.TreeView)
                {
                    Gtk.TreeView view = (sender as Widget).Parent as Gtk.TreeView;
                    view.GrabFocus();
                }
            }
        }
    }
}
