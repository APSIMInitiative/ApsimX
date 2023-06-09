using DocumentFormat.OpenXml.Drawing.Diagrams;
using Gtk;
using System;
using UserInterface.EventArguments.DirectedGraph;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    public class PlaylistView : ViewBase
    {

        public IEditorView editorView { get; private set; } = null;

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public PlaylistView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.PlaylistView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            editorView = new EditorView(owner);

            VBox vbox = (VBox)builder.GetObject("vbox");
            vbox.Add((editorView as ViewBase).MainWidget);
        }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                (editorView as EditorView).Dispose();

                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}