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


        public static DragAction GetAction(this DragContext args)
        {

            return args.SelectedAction;

        }


    }
}
