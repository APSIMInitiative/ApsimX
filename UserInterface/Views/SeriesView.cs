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
    public delegate void DataSourceChangedDelegate(string NewDataSource);
    public interface ISeriesView
    {
        /// <summary>
        /// Invokedn when the data source has changed by user.
        /// </summary>
        event DataSourceChangedDelegate DataSourceChanged;

        /// <summary>
        /// Get the Series grid.
        /// </summary>
        IGridView SeriesGrid { get; }

        /// <summary>
        /// Get the data grid.
        /// </summary>
        IGridView DataGrid { get; }

        /// <summary>
        /// Get or set the data source items.
        /// </summary>
        string[] DataSourceItems { get; set; }

        /// <summary>
        /// Get or set the focus on X variable list.
        /// </summary>
        bool XFocused { get; set; }

        /// <summary>
        /// Get or set the focus on Y variable list.
        /// </summary>
        bool YFocused { get; set; }

        /// <summary>
        /// Get or set the data source.
        /// </summary>
        string DataSource { get; set; }
    }


    public partial class SeriesView : UserControl, ISeriesView
    {
        /// <summary>
        /// Invokedn when the data source has changed by user.
        /// </summary>
        public event DataSourceChangedDelegate DataSourceChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public SeriesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Get the Series grid
        /// </summary>
        public IGridView SeriesGrid { get { return SeriesGridView; } }

        /// <summary>
        /// Get the data grid
        /// </summary>
        public IGridView DataGrid { get { return DataGridView; } }

        /// <summary>
        /// Get or set the data source items.
        /// </summary>
        public string[] DataSourceItems
        {
            get
            {
                List<string> Items = new List<string>();
                foreach (string Item in DataSourceCombo.Items)
                    Items.Add(Item);
                return Items.ToArray();
            }
            set
            {
                DataSourceCombo.Items.Clear();
                if (value != null && value.Length > 0)
                {
                    DataSourceCombo.Items.AddRange(value);
                    DataSourceCombo.Text = value[0];
                }
            }
        }

        /// <summary>
        /// Get or set the data source.
        /// </summary>
        public string DataSource
        {
            get
            {
                return DataSourceCombo.Text;
            }
            set
            {
                DataSourceCombo.Text = value;
            }
        }

        /// <summary>
        /// Get or set the focus on X variable list.
        /// </summary>
        public bool XFocused
        {
            get
            {
                return XRadio.Checked;
            }
            set
            {
                XRadio.Checked = value;
                if (value)
                    XRadio.Font = new Font(XRadio.Font, FontStyle.Bold);
                else
                    XRadio.Font = new Font(XRadio.Font, FontStyle.Regular);
            }
        }

        /// <summary>
        /// Get or set the focus on Y variable list.
        /// </summary>
        public bool YFocused
        {
            get
            {
                return YRadio.Checked;
            }
            set
            {
                YRadio.Checked = value;
                if (value)
                    YRadio.Font = new Font(XRadio.Font, FontStyle.Bold);
                else
                    YRadio.Font = new Font(XRadio.Font, FontStyle.Regular);
            }
        }

        /// <summary>
        /// User has changed the data source combo.
        /// </summary>
        private void OnDataSourceComboChanged(object sender, EventArgs e)
        {
            if (DataSourceChanged != null)
                DataSourceChanged(DataSourceCombo.Text);
        }

        private void OnXListBoxClick(object sender, EventArgs e)
        {
            XFocused = true;
            YFocused = false;
        }

        private void OnYListBoxClick(object sender, EventArgs e)
        {
            XFocused = false;
            YFocused = true;

        }





    }
}
