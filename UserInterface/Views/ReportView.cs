using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    public delegate void StringDelegate(string St);

    interface IReportView
    {
        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        IEditorView VariableList { get; }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        IEditorView EventList { get; }

        /// <summary>
        /// Provides access to the DataGrid.
        /// </summary>
        IGridView DataGrid { get; }

        /// <summary>
        /// This event is fired when the results count changes.
        /// </summary>
        event EventHandler<PageEventArgs> OnPageDataChanged;

        void SetResultsPerPage(int count);
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
        /// Invoked when the user changes the count text.
        /// </summary>
        public event EventHandler<PageEventArgs> OnPageDataChanged;

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        public IEditorView VariableList { get { return VariableEditor; } }

        /// <summary>
        /// Provides access to the variable list.
        /// </summary>
        public IEditorView EventList { get { return FrequencyEditor; } }

        /// <summary>
        /// Provides access to the DataGrid.
        /// </summary>
        public IGridView DataGrid { get { return GridView; } }

        /// <summary>
        /// Sets the number of results that will appear per page.
        /// </summary>
        /// <param name="count"></param>
        public void SetResultsPerPage(int count)
        {
            if (count == 0)
                tbCount.Text = "";
            else
                tbCount.Text = count.ToString();
        }

        private void tbCount_TextChanged(object sender, EventArgs e)
        {
            PageEventArgs args = new PageEventArgs();
            int convert = -1;
            args.count = -1;

            if(tbCount.Text=="")
                args.count = 0;
            else if (Int32.TryParse(tbCount.Text, out convert))
                args.count = convert;

            if (args.count != -1)
            {
                args.gotoStart = true;
                OnPageDataChanged.Invoke(this, args);
            }
        }

        private void bHome_Click(object sender, EventArgs e)
        {
            PageEventArgs args = new PageEventArgs();
            int convert;

            if (Int32.TryParse(tbCount.Text, out convert))
            {
                args.count = convert;
                args.gotoStart = true;
                args.goForward = true;
                OnPageDataChanged.Invoke(this, args);
            }
        }

        private void bBack_Click(object sender, EventArgs e)
        {
            PageEventArgs args = new PageEventArgs();
            int convert;

            if (Int32.TryParse(tbCount.Text, out convert))
            {
                args.count = convert;
                args.gotoStart = false;
                args.goForward = false;
                OnPageDataChanged.Invoke(this, args);
            }
        }

        private void bForward_Click(object sender, EventArgs e)
        {
            PageEventArgs args = new PageEventArgs();
            int convert;

            if (Int32.TryParse(tbCount.Text, out convert))
            {
                args.count = convert;
                args.gotoStart = false;
                args.goForward = true;
                OnPageDataChanged.Invoke(this, args);
            }
        }

        private void bEnd_Click(object sender, EventArgs e)
        {
            //disabled for now as it will require extra data from the database
            //and is not really useful.
        }
    }

    /// <summary>
    /// An event handler for data page changes
    /// </summary>
    public class PageEventArgs : EventArgs
    {
        public int count { get; set; }
        public bool gotoStart { get; set; }
        public bool goForward { get; set; }
    }

}
