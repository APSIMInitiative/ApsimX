﻿

namespace Utility
{
    using Gtk;
    using UserInterface.Views;
    using UserInterface.Presenters;
    using UserInterface.Extensions;

    public class StringEntryForm
    {
        /// <summary>Show dialog box</summary>
        public static string ShowDialog(ExplorerPresenter explorerPresenter, string caption, string labelText, string defaultText)
        {
            Gtk.Window topLevelWindow = explorerPresenter.GetView().MainWidget.Toplevel as Gtk.Window;

            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.StringEntryForm.glade");
            Dialog dialog = (Dialog)builder.GetObject("dialog");
            Label prompt = (Label)builder.GetObject("prompt");
            Entry entryBox = (Entry)builder.GetObject("entryBox");

            dialog.TransientFor = topLevelWindow;
            dialog.Title = caption;
            if (labelText != null)
                prompt.Text = labelText;
            if (defaultText != null)
                entryBox.Text = defaultText;
            entryBox.GrabFocus();
            dialog.ShowAll();
            int response = dialog.Run();
            string text = entryBox.Text;
            dialog.Dispose();

            if (response == 1)
                return text;
            else
                return null;
        }

    }

}

    
