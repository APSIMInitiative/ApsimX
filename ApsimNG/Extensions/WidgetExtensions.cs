using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;


using StateType = Gtk.StateFlags;


namespace UserInterface.Extensions
{
    /// <summary>
    /// Extension methods for Gtk widgets, generally provided for compatibility between
    /// gtk2 and gtk3 APIs. These should probably all be refactored out when we drop
    /// support for gtk2 builds.
    /// </summary>
    internal static class WidgetExtensions
    {
        public static void Cleanup(this Widget widget)
        {

            widget.Dispose();

        }

        public static CellRenderer[] GetCells(this TreeViewColumn column)
        {

            return column.Cells;

        }

        public static string GetActiveText(this ComboBox combo)
        {

            if (combo.GetActiveIter(out TreeIter iter))
                return combo.Model.GetValue(iter, 0)?.ToString();

            return null;

        }

        public static Settings GetSettings(this Widget widget)
        {

            return Settings.Default;

        }

        // Functions provided for backwards-compatibility with shared gtk2 code.
        public static Gdk.Color GetForegroundColour(this Widget widget, StateType state)
        {

            return widget.StyleContext.GetColor(state).ToGdkColor();

        }

        // Functions provided for backwards-compatibility with shared gtk2 code.
//
//        [Obsolete("Use gtk_render_background() instead")]
//
        public static Gdk.Color GetBackgroundColour(this Widget widget, StateType state)
        {
#pragma warning disable 0612 // fixme
            return widget.StyleContext.GetBackgroundColor(state).ToGdkColor();
#pragma warning restore 0612
        }

        /// <summary>
        /// Set the progress bar's fraction complete.
        /// </summary>
        /// <param name="progress">Progress bar to be modified.</param>
        /// <param name="fraction">Fraction complete (in range [0, 1]).</param>
        public static void SetFractionComplete(this ProgressBar progress, double fraction)
        {

            progress.Fraction = fraction;

        }

        public static double GetFractionComplete(this ProgressBar progress)
        {

            return progress.Fraction;

        }

        /// <summary>
        /// Create a ProgressBar widget with default values setup.
        /// </summary>
        /// <remarks>
        /// gtk2 compat function.
        /// </remarks>
        public static ProgressBar CreateProgressBar()
        {

            return new ProgressBar();

        }

        /// <summary>
        /// Mimics the old gtk2 TextView.GetIterAtLocation(int, int) function.
        /// </summary>
        /// <param name="view">Some GtkTextView.</param>
        /// <param name="x">Row number?</param>
        /// <param name="y">Column number?</param>
        /// <returns></returns>
        public static TextIter GetIterAtLocation(this TextView view, int x, int y)
        {

            if (view.GetIterAtLocation(out TextIter iter, x, y))
                return iter;

            return TextIter.Zero;

        }

        /// <summary>
        /// Set an adjustment's value.
        /// </summary>
        /// <param name="adjustment">The adjustment to be adjusted.</param>
        /// <param name="value">The new value.</param>
        public static void SetValue(this Adjustment adjustment, double value)
        {
            if (value > 0 && value < adjustment.Upper)
            {
                adjustment.Value = value;

            }
        }


        /// <summary>
        /// Set the font name of a font chooser dialog.
        /// </summary>
        /// <param name="fontChooser"></param>
        /// <param name="fontName"></param>
        public static void SetFontName(this FontChooserDialog fontChooser, string fontName)
        {
            fontChooser.Font = fontName;
        }


        /// <summary>
        /// Returns a widget's Gdk.Window.
        /// </summary>
        /// <param name="widget">A widget.</param>
        public static Gdk.Window GetGdkWindow(this Widget widget)
        {

            return widget.Window;

        }


        /// <summary>
        /// Mimics the GtkTable.Attach() method for gtk2 compat
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="child"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="xOptions"></param>
        /// <param name="yOptions"></param>
        /// <param name="xPadding"></param>
        /// <param name="yPadding"></param>
        public static void Attach(this Grid grid, Widget child, int left, int right, int top, int bottom, AttachOptions xOptions, AttachOptions yOptions, int xPadding, int yPadding)
        {
            grid.Attach(child, left, top, right - left, bottom - top);
        }


        public static MenuItem CreateImageMenuItem(string text, Image image)
        {

            // GtkImageMenuItem has been deprecated since GTK+ 3.10. If you want to
            // display an icon in a menu item, you should use GtkMenuItem and pack a GtkBox
            // with a GtkImage and a GtkLabel instead.
            HBox container = new HBox();
            Label label = new Label(text);
            MenuItem imageItem = new MenuItem();

            container.PackStart(image, false, false, 0);
            container.PackStart(label, false, false, 0);
            imageItem.Add(container);

            return imageItem;
        }


        /// <summary>
        /// Gets the child of grid whose area covers the grid cell whose upper left corner is at (left, top).
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="left">Column index.</param>
        /// <param name="top">Row index.</param>
        /// <remarks>Provided for compatibility with gtk2 builds which use GtkTable.</remarks>
        public static Widget GetChild(this Grid grid, int left, int top)
        {
            return grid.GetChildAt(left, top);
        }

    }
}
