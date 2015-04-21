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
namespace UserInterface.Views
{
    /// <summary>
    /// An interface for a HTML view.
    /// </summary>
    interface IHTMLView
    {
        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        void AddContextAction(string ButtonText, System.EventHandler OnClick);

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        string MemoText { get; set; }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        bool ReadOnly { get; set; }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// </summary>
    public partial class HTMLView : UserControl, IHTMLView
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public HTMLView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get 
            {
                return RTFToHTML.Convert(richTextBox1.Rtf);
            }
            set 
            {
                if (value != null)
                    richTextBox1.Rtf = HTMLToRTF.Convert(value);
            }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get 
            { 
                return richTextBox1.ReadOnly; 
            }
            set 
            { 
                richTextBox1.ReadOnly = value;
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
        }

        #region Event Handlers
        /// <summary>
        /// User has clicked bold.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnBoldClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionFont.Bold)
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
            else
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
        }
       
        /// <summary>
        /// User has clicked italics.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnItalicClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionFont.Italic)
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
            else
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Italic);
        }
        /// <summary>
        /// User has clicked underline
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnUnderlineClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionFont.Underline)
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
            else
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Underline);
        }

        /// <summary>
        /// User has clicked strikethrough
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnStrikeThroughClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionFont.Strikeout)
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
            else
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Strikeout);
        }

        /// <summary>
        /// User has clicked superscript
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnSuperscriptClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionCharOffset == 0)
                richTextBox1.SelectionCharOffset = 8;
            else
                richTextBox1.SelectionCharOffset = 0;
        }

        /// <summary>
        /// User has clicked subscript.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnSubscriptClick(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionCharOffset == 0)
                richTextBox1.SelectionCharOffset = -8;
            else
                richTextBox1.SelectionCharOffset = 0;
        }

        /// <summary>
        /// User has changed the heading.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnHeadingChanged(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionFont != null)
            {
                if (headingComboBox.Text == "Heading 1")
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, 16f);
                else if (headingComboBox.Text == "Heading 2")
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, 14f);
                else if (headingComboBox.Text == "Heading 3")
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, 12f);
                else
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, 10f, richTextBox1.SelectionFont.Style);
            }
        }

        /// <summary>
        /// User has changed the selection.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            richTextBox1.TextChanged -= OnHeadingChanged;
            if (richTextBox1.SelectionFont == null)
                headingComboBox.Text = "Normal";
            else if (richTextBox1.SelectionFont.Size == 16f)
                headingComboBox.Text = "Heading 1";
            else if (richTextBox1.SelectionFont.Size == 14f)
                headingComboBox.Text = "Heading 2";
            else if (richTextBox1.SelectionFont.Size == 12f)
                headingComboBox.Text = "Heading 3";
            else if (richTextBox1.SelectionFont.Size == 10f)
                headingComboBox.Text = "Normal";

            richTextBox1.TextChanged += OnHeadingChanged;
        }

        /// <summary>
        /// User has clicked a link.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnLinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        #endregion


    }

}
