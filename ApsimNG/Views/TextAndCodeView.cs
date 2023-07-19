using DocumentFormat.OpenXml.Drawing.Diagrams;
using Gtk;
using System;
using UserInterface.EventArguments.DirectedGraph;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    public class TextAndCodeView : ViewBase
    {

        public IEditorView editorView { get; private set; } = null;

        private Gtk.Label label1;

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public TextAndCodeView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.TextAndCodeView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            editorView = new EditorView(owner);

            VBox vbox = (VBox)builder.GetObject("vbox");
            vbox.Add((editorView as ViewBase).MainWidget);

            label1 = (Label)builder.GetObject("label1");
        }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="text"></param>
        public void SetLabelText(string text)
        {
            label1.Text = text;
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