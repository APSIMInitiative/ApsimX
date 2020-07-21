using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gdk;

namespace UserInterface.Extensions
{
    /// <summary>
    /// Extension methods for Gdk objects.
    /// </summary>
    internal static class GdkExtensions
    {
#if NETCOREAPP
        public static Color ToGdkColor(this RGBA colour)
        {
            return new Color((byte)(colour.Red * 0xff), (byte)(colour.Green * 0xff), (byte)(colour.Blue * 0xff));
        }

        [Obsolete("Use window.Width and window.Height properties")]
        public static void GetSize(this Window window, out int width, out int height)
        {
            width = window.Width;
            height = window.Height;
        }
#endif

        public static DragAction GetAction(this DragContext args)
        {
#if NETFRAMEWORK
            return args.Action;
#else
            return args.SelectedAction;
#endif
        }

#if NETFRAMEWORK
        /// <summary>
        /// Yes, it's technically not a gdk type.
        /// </summary>
        public static Cairo.Surface GetTarget(this Cairo.Context context)
        {
            return context.Target;
        }
#endif
    }
}
