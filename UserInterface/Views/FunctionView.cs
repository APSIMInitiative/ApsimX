
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

        public FunctionView()
        {
            InitializeComponent();

            gridView.CellsChanged += new EventHandler<GridCellsChangedArgs>(gridViewCellsChanged);
        }

        void gridViewCellsChanged(object sender, GridCellsChangedArgs e)
        {
            UpdateChart();
        }

        void UpdateChart()
        {
           // graph.


        }

        
    }
}
