
namespace UserInterface.Views
{
    using Gtk;

    interface IReportActivityLedgerView
    {
        /// <summary>Provides access to the DataGrid.</summary>
        ViewBase DataStoreView { get; }

        /// <summary>Provides access to the DataGrid.</summary>
        IActivityLedgerGridView DisplayView { get; }
    }

    public class ReportActivityLedgerView : ViewBase, IReportActivityLedgerView
    {
        private Notebook notebook1 = null;
        private Alignment alignment1 = null;
        private Alignment alignment2 = null;

        private ViewBase dataStoreView1;
        private ActivityLedgerGridView displayView1;

        /// <summary>Constructor</summary>
        public ReportActivityLedgerView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ReportActivityLedgerView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            alignment1 = (Alignment)builder.GetObject("alignment1");
            alignment2 = (Alignment)builder.GetObject("alignment2");
            mainWidget = notebook1;

            dataStoreView1 = new ViewBase(this, "ApsimNG.Resources.Glade.DataStoreView.glade");
            alignment1.Add(dataStoreView1.MainWidget);

            displayView1 = new ActivityLedgerGridView(this);
            alignment2.Add(displayView1.MainWidget);

            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            dataStoreView1 = null;
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        /// <summary>Provides access to the DataGrid.</summary>
        public ViewBase DataStoreView { get { return dataStoreView1; } }
        /// <summary>Provides access to the display Grid.</summary>
        public IActivityLedgerGridView DisplayView { get { return displayView1; } }
    }
}
