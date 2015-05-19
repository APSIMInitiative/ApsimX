
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Windows.Forms;
    using Classes;
    using DataGridViewAutoFilter;
    using EventArguments;
    using Interfaces;

    /// <summary>
    /// An interface for a Function view.
    /// </summary>
    interface IFunctionView
    {

        IGridView gridView { get; }
        IGridView gridViewXProp { get; }
        //IGraphView graphView { get; }
    }

    public partial class FunctionView : UserControl, IFunctionView
    {
        public IGridView gridView
        {
            get
            {
                return grid;
            }
        }

        public IGridView gridViewXProp
        {
            get
            {
                return gridXProp;
            }
        }

        //public IGraphView graphView
        //{
        //    get
        //    {
        //        return graph;
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        public FunctionView()
        {
            InitializeComponent();

            grid.CellsChanged += new EventHandler<GridCellsChangedArgs>(gridViewCellsChanged);

           // grid.
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void gridViewCellsChanged(object sender, GridCellsChangedArgs e)
        {
            UpdateChart();
        }
        /// <summary>
        /// 
        /// </summary>
        void UpdateChart()
        {
            chart1.Series[0].Points.Clear();

            DataTable dt = grid.DataSource;

            for (int i = 0; i < dt.Rows.Count; i++)
            {

                chart1.Series[0].Points.AddXY(double.Parse(dt.Rows[i][0].ToString()), double.Parse(dt.Rows[i][1].ToString()));
            }

        }

        
    }
}
