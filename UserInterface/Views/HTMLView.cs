using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using UserInterface.Forms;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using UserInterface.Classes;
using System.Diagnostics;
using MigraDoc.RtfRendering;

namespace UserInterface.Views
{
    /// <summary>
    /// An interface for a HTML view.
    /// </summary>
    interface IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        string ImagePath { get; set; }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        void SetContents(string contents, bool allowModification);

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        string GetMarkdown();

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        void UseMonoSpacedFont();
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// </summary>
    public partial class HTMLView : UserControl, IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public HTMLView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        public void SetContents(string contents, bool allowModification)
        {
            bool editingEnabled = false;
            if (contents != null)
            {
                textBox1.Text = contents;
                editingEnabled = PopulateView(contents, editingEnabled);
            }

            if (!editingEnabled)
            {
                richTextBox1.ContextMenuStrip = null;
                textBox1.ContextMenuStrip = null;
            }
            TurnEditorOn(false);
        }

        /// <summary>
        /// Populate the view given the specified text.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="editingEnabled"></param>
        /// <returns></returns>
        private bool PopulateView(string contents, bool editingEnabled)
        {
            if (contents.Contains("@{\rtf"))
                richTextBox1.Rtf = contents;
            else
            {
                var doc = new MigraDoc.DocumentObjectModel.Document();
                var section = doc.AddSection();
                if (contents.Contains("<html>"))
                    HtmlToMigraDoc.Convert(contents, section, ImagePath);
                else
                {
                    MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
                    markDown.ExtraMode = true;
                    string html = markDown.Transform(contents);
                    editingEnabled = true;
                    HtmlToMigraDoc.Convert(html, section, ImagePath);
                }
                RtfDocumentRenderer renderer = new RtfDocumentRenderer();
                richTextBox1.Rtf = renderer.RenderToString(doc, null);
            }

            return editingEnabled;
        }

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        public string GetMarkdown()
        {
            return textBox1.Text;
        }

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        public void UseMonoSpacedFont() 
        {
            richTextBox1.Font = new Font("Consolas", 10F);   
        }

        /// <summary>
        /// Turn the editor on or off.
        /// </summary>
        /// <param name="turnOn"></param>
        private void TurnEditorOn(bool turnOn)
        {
            richTextBox1.Visible = !turnOn;
            textBox1.Visible = turnOn;
            richTextBox1.Dock = DockStyle.Fill;
            textBox1.Dock = DockStyle.Fill;

            menuItem1.Visible = !turnOn;
            menuItem2.Visible = turnOn;
        }

        #region Event Handlers

        /// <summary>
        /// User has clicked a link.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnLinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        /// <summary>
        /// User has clicked 'edit'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEditClick(object sender, EventArgs e)
        {
            TurnEditorOn(true);
        }

        /// <summary>
        /// User has clicked 'preview'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewClick(object sender, EventArgs e)
        {
            TurnEditorOn(false);
            PopulateView(textBox1.Text, true);
        }

        #endregion

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.apsim.info/Documentation/APSIM(nextgeneration)/Memo.aspx");
        }
    }
}
