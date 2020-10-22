using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

#if NETCOREAPP
using StateType = Gtk.StateFlags;
#endif

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
#if NETFRAMEWORK
            widget.Destroy();
#else
            widget.Dispose();
#endif
        }

        public static CellRenderer[] GetCells(this TreeViewColumn column)
        {
#if NETFRAMEWORK
            return column.CellRenderers;
#else
            return column.Cells;
#endif
        }

        public static string GetActiveText(this ComboBox combo)
        {
#if NETFRAMEWORK
            return combo.ActiveText;
#else
            if (combo.GetActiveIter(out TreeIter iter))
                return combo.Model.GetValue(iter, 0)?.ToString();

            return null;
#endif
        }

        public static Settings GetSettings(this Widget widget)
        {
#if NETFRAMEWORK
            return widget.Settings;
#else
            return Settings.Default;
#endif
        }

        // Functions provided for backwards-compatibility with shared gtk2 code.
        public static Gdk.Color GetForegroundColour(this Widget widget, StateType state)
        {
#if NETFRAMEWORK
            return widget.Style.Foreground(state);
#else
            return widget.StyleContext.GetColor(state).ToGdkColor();
#endif
        }

        // Functions provided for backwards-compatibility with shared gtk2 code.
//#if NETCOREAPP
//        [Obsolete("Use gtk_render_background() instead")]
//#endif
        public static Gdk.Color GetBackgroundColour(this Widget widget, StateType state)
        {
#if NETFRAMEWORK
            return widget.Style.Background(state);
#else
            return widget.StyleContext.GetBackgroundColor(state).ToGdkColor();
#endif
        }

        /// <summary>
        /// Set the progress bar's fraction complete.
        /// </summary>
        /// <param name="progress">Progress bar to be modified.</param>
        /// <param name="fraction">Fraction complete (in range [0, 1]).</param>
        public static void SetFractionComplete(this ProgressBar progress, double fraction)
        {
#if NETCOREAPP
            progress.Fraction = fraction;
#else
            progress.Adjustment.Value = fraction;
#endif
        }

        public static double GetFractionComplete(this ProgressBar progress)
        {
#if NETCOREAPP
            return progress.Fraction;
#else
            return progress.Adjustment.Value;
#endif
        }

        /// <summary>
        /// Create a ProgressBar widget with default values setup.
        /// </summary>
        /// <remarks>
        /// gtk2 compat function.
        /// </remarks>
        public static ProgressBar CreateProgressBar()
        {
#if NETFRAMEWORK
            return new ProgressBar(new Adjustment(0, 0, 1, 0.01, 0.01, 1));
#else
            return new ProgressBar();
#endif
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
#if NETFRAMEWORK
            return view.GetIterAtLocation(x, y);
#else
            if (view.GetIterAtLocation(out TextIter iter, x, y))
                return iter;

            return TextIter.Zero;
#endif
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
#if NETFRAMEWORK
                adjustment.ChangeValue();
#endif
            }
        }

#if NETCOREAPP
        /// <summary>
        /// Set the font name of a font chooser dialog.
        /// </summary>
        /// <param name="fontChooser"></param>
        /// <param name="fontName"></param>
        public static void SetFontName(this FontChooserDialog fontChooser, string fontName)
        {
            fontChooser.Font = fontName;
        }
#endif

        /// <summary>
        /// Returns a widget's Gdk.Window.
        /// </summary>
        /// <param name="widget">A widget.</param>
        public static Gdk.Window GetGdkWindow(this Widget widget)
        {
#if NETFRAMEWORK
            return widget.GdkWindow;
#else
            return widget.Window;
#endif
        }

#if NETCOREAPP
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

        public static void Attach(this Grid grid, Widget child, int left, int right, int top, int bottom)
        {
            grid.Attach(child, left, top, right - left, bottom - top);
        }
#endif

        public static MenuItem CreateImageMenuItem(string text, Image image)
        {
#if NETFRAMEWORK
            ImageMenuItem imageItem = new ImageMenuItem(text);
            imageItem.Image = image;
#else
            // GtkImageMenuItem has been deprecated since GTK+ 3.10. If you want to
            // display an icon in a menu item, you should use GtkMenuItem and pack a GtkBox
            // with a GtkImage and a GtkLabel instead.
            HBox container = new HBox();
            Label label = new Label(text);
            MenuItem imageItem = new MenuItem();

            container.PackStart(image, false, false, 0);
            container.PackStart(label, false, false, 0);
            imageItem.Add(container);
#endif
            return imageItem;
        }

#if NETCOREAPP
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
#endif
    }
}
