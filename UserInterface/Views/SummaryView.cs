using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{
    public interface ISummaryView
    {
        /// <summary>
        /// Set the summary contents.
        /// </summary>
        void SetSummary(string contents, bool html);

        bool html { get; set; }
        bool AutoCreate { get; set; }
        bool StateVariables { get; set; }

        event EventHandler HTMLChanged;
        event EventHandler AutoCreateChanged;
        event EventHandler StateVariablesChanged;
        event EventHandler CreateButtonClicked;
    }

    public partial class SummaryView : UserControl, ISummaryView
    {
        public event EventHandler HTMLChanged;
        public event EventHandler AutoCreateChanged;
        public event EventHandler StateVariablesChanged;
        public event EventHandler CreateButtonClicked;

        /// <summary>
        /// Contructor
        /// </summary>
        public SummaryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the summary contents.
        /// </summary>
        public void SetSummary(string contents, bool html)
        {
            if (html)
            {
                TextBox.Visible = false;
                HtmlControl.Visible = true;
                HtmlControl.MemoText = contents;
                HtmlControl.ReadOnly = true;
            }
            else
            {
                HtmlControl.Visible = false;
                TextBox.Dock = DockStyle.Fill;
                TextBox.Visible = true;
                TextBox.Text = contents;
            }
        }

        /// <summary>
        /// HTML checkbox property
        /// </summary>
        public bool html
        {
            get
            {
                return HTMLCheckBox.Checked;
            }
            set
            {
                HTMLCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// Auto create checkbox property
        /// </summary>
        public bool AutoCreate
        {
            get
            {
                return AutoCreateCheckBox.Checked;
            }
            set
            {
                AutoCreateCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// state variables checkbox property
        /// </summary>
        public bool StateVariables
        {
            get
            {
                return StateVariablesCheckBox.Checked;
            }
            set
            {
                StateVariablesCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// Create button was clicked.
        /// </summary>
        private void OnCreateButtonClick(object sender, EventArgs e)
        {
            if (CreateButtonClicked != null)
                CreateButtonClicked.Invoke(this, e);
        }

        /// <summary>
        /// User has changed the html checkbox.
        /// </summary>
        private void OnHTMLCheckBoxChanged(object sender, EventArgs e)
        {
            if (HTMLChanged != null)
                HTMLChanged.Invoke(this, e);
        }

        /// <summary>
        /// User has changed the auto create checkbox.
        /// </summary>
        private void OnAutoCreateCheckBoxChanged(object sender, EventArgs e)
        {
            if (AutoCreateChanged != null)
                AutoCreateChanged.Invoke(this, e);
        }

        /// <summary>
        /// User has changed the state variables checkbox.
        /// </summary>
        private void OnStateVariablesCheckBoxChanged(object sender, EventArgs e)
        {
            if (StateVariablesChanged != null)
                StateVariablesChanged.Invoke(this, e);
        }   
    }
}
