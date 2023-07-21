using Gtk;
using System;
using System.Reflection.Emit;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    public class TextAndCodeView : ViewBase
    {

        public IEditorView editorView { get; private set; } = null;

        private Gtk.Label label1;

        private Gtk.Label label2;

        private Gtk.VPaned box1;

        private Gtk.HPaned box2;

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public TextAndCodeView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.TextAndCodeView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            label1 = (Gtk.Label)builder.GetObject("label1");
            label2 = (Gtk.Label)builder.GetObject("label2");
            editorView = new EditorView(owner);

            label1.LineWrap = true;
            label2.LineWrap = true;

            ScrolledWindow sw = (ScrolledWindow)builder.GetObject("scrolledwindow2");
            sw.Add((editorView as ViewBase).MainWidget);

            box1 = (VPaned)builder.GetObject("vpaned1");
            box1.Position = (int)Math.Round(this.owner.MainWidget.AllocatedHeight * 0.8);

            box2 = (HPaned)builder.GetObject("hpaned2");
            box2.Position = (int)Math.Round(this.owner.MainWidget.AllocatedWidth * 0.5);
        }

        /// <summary></summary>
        /// <param name="text"></param>
        public void SetLabelText(string text)
        {
            label1.Markup = text;
        }

        /// <summary></summary>
        /// <param name="text"></param>
        public void SetOutputText(string text)
        {
            label2.Text = text;
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