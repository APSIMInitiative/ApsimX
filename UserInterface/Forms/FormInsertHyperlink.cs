using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Forms
{
    public partial class FormInsertHyperlink : Form
    {
        public FormInsertHyperlink()
        {
            InitializeComponent();

            onEditTextChanged();
        }

        /// <summary>
        /// This property wraps the 'URL' edit box
        /// </summary>
        internal string url
        {
            get { return editUrl.Text; }
            set { editUrl.Text = value; }
        }

        /// <summary>
        /// This property wraps the 'Visible Text' edit box
        /// </summary>
        internal string visibleText
        {
            get { return editVisibleText.Text; }
            set { editVisibleText.Text = value; }
        }

        private void editUrl_TextChanged(object sender, EventArgs e)
        {
            onEditTextChanged();
        }

        private void editVisibleText_TextChanged(object sender, EventArgs e)
        {
            onEditTextChanged();
        }

        void onEditTextChanged()
        {
            //enable the OK button if and only if both edit boxes contain some text
            buttonOk.Enabled = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(visibleText);
        }
    }
}
