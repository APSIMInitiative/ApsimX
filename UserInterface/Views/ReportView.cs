using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{

    public delegate void StringDelegate(string St);

    interface IReportView
    {
        /// <summary>
        /// Invoked when the user clicks on the autocreate checkbox.
        /// </summary>
        event EventHandler OnAutoCreateClick;

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        Utility.IEditor VariableList { get; }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        Utility.IEditor EventList { get; }

        /// <summary>
        /// Provides access to the DataGrid.
        /// </summary>
        IGridView DataGrid { get; }

        /// <summary>
        /// Provides access to the autocreate checkbox.
        /// </summary>
        bool AutoCreate { get; set; }
    }



    public partial class ReportView : UserControl, IReportView
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ReportView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        public Utility.IEditor VariableList { get { return VariableEditor; } }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        public Utility.IEditor EventList { get { return FrequencyEditor; } }

        /// <summary>
        /// Provides access to the DataGrid.
        /// </summary>
        public IGridView DataGrid { get { return GridView; } }

        /// <summary>
        /// Invoked when the user clicks on the autocreate checkbox.
        /// </summary>
        public event EventHandler OnAutoCreateClick;

        /// <summary>
        /// Provides access to the autocreate checkbox.
        /// </summary>
        public bool AutoCreate
        {
            get
            {
                return AutoCheckBox.Checked;
            }
            set
            {
                AutoCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// User has clicked the auto create checkbox.
        /// </summary>
        private void OnAutoCheckBoxChanged(object sender, EventArgs e)
        {
            if (OnAutoCreateClick != null)
                OnAutoCreateClick(null, null);
        }
    }

}
