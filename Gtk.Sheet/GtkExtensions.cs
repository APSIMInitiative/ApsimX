using Gdk;

namespace Gtk.Sheet
{
    internal static class GtkExtensions
    {
        internal static SheetEventKey ToSheetEventKey(this EventKey evnt)
        {
            var keyParams = new SheetEventKey();
            if (evnt.Key == Gdk.Key.Left)
                keyParams.Key = Keys.Left;
            else if (evnt.Key == Gdk.Key.Up)
                keyParams.Key = Keys.Up;
            else if (evnt.Key == Gdk.Key.Right)
                keyParams.Key = Keys.Right;
            else if (evnt.Key == Gdk.Key.Down)
                keyParams.Key = Keys.Down;
            else if (evnt.Key == Gdk.Key.Page_Up)
                keyParams.Key = Keys.PageUp;
            else if (evnt.Key == Gdk.Key.Page_Down)
                keyParams.Key = Keys.PageDown;
            else if (evnt.Key == Gdk.Key.period)
                keyParams.Key = Keys.Period;
            else if (evnt.Key == Gdk.Key.Return || evnt.Key == Gdk.Key.KP_Enter
                || evnt.Key == Gdk.Key.ISO_Enter)
                keyParams.Key = Keys.Return;
            else if (evnt.Key == Gdk.Key.Home)
                keyParams.Key = Keys.Home;
            else if (evnt.Key == Gdk.Key.End)
                keyParams.Key = Keys.End;
            else if (evnt.Key == Gdk.Key.Delete)
                keyParams.Key = Keys.Delete;
            else if (evnt.Key == Gdk.Key.Tab || evnt.Key == Gdk.Key.KP_Tab
                || evnt.Key == Gdk.Key.ISO_Left_Tab)
                keyParams.Key = Keys.Tab;
            else
            {
                string keyName = char.ConvertFromUtf32((int)Gdk.Keyval.ToUnicode((uint)evnt.KeyValue));
                if (!char.TryParse(keyName, out keyParams.KeyValue))
                    // Fallback to previous behaviour. This shouldn't usually happen, but it's probably
                    // possible, and this fallback is unlikely to work successfully. E.g. on my keyboard,
                    // the KeyValue from numpad 1, when cast to char, yields 'ﾱ'.
                    keyParams.KeyValue = (char)evnt.KeyValue;
            }

            keyParams.Control = evnt.State == ModifierType.ControlMask;
            keyParams.Shift = evnt.State == ModifierType.ShiftMask;
            return keyParams;
        }

        internal static SheetEventButton ToSheetEventButton(this EventButton evnt)
        {
            var buttonParams = new SheetEventButton()
            {
                X = (int)evnt.X,
                Y = (int)evnt.Y,
                LeftButton = evnt.Button == 1,
                Shift = evnt.State == ModifierType.ShiftMask
            };
            return buttonParams;
        }
    
    }
}
