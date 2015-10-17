// -----------------------------------------------------------------------
// <copyright file="ReportView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System.Windows.Forms;

    interface IReportView
    {
        /// <summary>Provides access to the variable list.</summary>
        IEditorView VariableList { get; }

        /// <summary>Provides access to the variable list.</summary>
        IEditorView EventList { get; }

        /// <summary>Provides access to the DataGrid.</summary>
        IDataStoreView DataStoreView { get; }
    }

    public partial class ReportView : UserControl, IReportView
    {
        /// <summary>Constructor</summary>
        public ReportView()
        {
            InitializeComponent();
        }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView VariableList { get { return VariableEditor; } }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView EventList { get { return FrequencyEditor; } }

        /// <summary>Provides access to the DataGrid.</summary>
        public IDataStoreView DataStoreView { get { return dataStoreView1; } }
    }
}
