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
        /// Provides access to the variable list.
        /// </summary>
        Utility.IEditor VariableList { get; }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        Utility.IEditor EventList { get; }
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

    }
}
