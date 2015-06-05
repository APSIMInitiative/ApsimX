
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
        IGraphView graphView { get; }
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

        public IGraphView graphView
        {
            get
            {
                return graph;
            }
        }

        public FunctionView()
        {
            InitializeComponent();

            grid.CellsChanged += new EventHandler<GridCellsChangedArgs>(gridViewCellsChanged);
        }

        void gridViewCellsChanged(object sender, GridCellsChangedArgs e)
        {
            //UpdateChart();
        }

        //void UpdateChart()
        //{
           

        //}

        
    }
}
