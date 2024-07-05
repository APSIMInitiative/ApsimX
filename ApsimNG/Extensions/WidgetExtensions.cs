using Gtk;

namespace UserInterface.Extensions
{
    /// <summary>
    /// Extension methods for Gtk widgets, generally provided for compatibility between
    /// gtk2 and gtk3 APIs. These should probably all be refactored out when we drop
    /// support for gtk2 builds.
    /// </summary>
    internal static class WidgetExtensions
    {
        public static string GetActiveText(this ComboBox combo)
        {
            if (combo.GetActiveIter(out TreeIter iter))
                return combo.Model.GetValue(iter, 0)?.ToString();
            return null;
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
        /// GtkImageMenuItem has been deprecated since GTK+ 3.10. If you want to
        /// display an icon in a menu item, you should use GtkMenuItem and pack a GtkBox
        /// with a GtkImage and a GtkLabel instead.
        /// </summary>
        /// <param name="text">Text to be displayed on the menu item.</param>
        /// <param name="image">Image to be displayed on the menu item.</param>
        public static MenuItem CreateImageMenuItem(string text, Image image)
        {
            Box container = new Box(Orientation.Horizontal, 0);
            Label label = new Label(text);
            MenuItem imageItem = new MenuItem();

            container.PackStart(image, false, false, 0);
            container.PackStart(label, false, false, 0);
            imageItem.Add(container);

            return imageItem;
        }
    }
}
