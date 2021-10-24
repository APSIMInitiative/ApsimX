using Gdk;

namespace UserInterface.Views
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
            else if (evnt.Key == Gdk.Key.Return)
                keyParams.Key = Keys.Return;

            keyParams.Control = evnt.State == ModifierType.ControlMask;
            return keyParams;
        }

        internal static SheetEventButton ToSheetEventButton(this EventButton evnt)
        {
            var buttonParams = new SheetEventButton();
            buttonParams.X = (int)evnt.X;
            buttonParams.Y = (int)evnt.Y;
            return buttonParams;
        }

    }
}
