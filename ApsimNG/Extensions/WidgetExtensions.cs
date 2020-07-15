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
#if NETCOREAPP
        [Obsolete("Use widget.StyleContext.GetColor()")]
#endif
        public static Gdk.Color GetForegroundColour(this Widget widget, StateType state)
        {
#if NETFRAMEWORK
            return widget.Style.Foreground(state);
#else
            return widget.StyleContext.GetColor(state).ToGdkColor();
#endif
        }

        // Functions provided for backwards-compatibility with shared gtk2 code.
#if NETCOREAPP
        [Obsolete("Use widget.StyleContext.GetColor()")]
#endif
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
    }
}
