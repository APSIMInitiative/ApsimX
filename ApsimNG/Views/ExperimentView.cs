namespace UserInterface.Views
{
    using Extensions;
    using Interfaces;
    using Gtk;
    using System;

    public class ExperimentView : ViewBase, IExperimentView
    {
        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public ExperimentView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ExperimentView.glade");
            mainWidget = (VBox)builder.GetObject("vbox");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            List = new ListView(owner, 
                                (Gtk.TreeView) builder.GetObject("list"),
                                (Gtk.Menu)builder.GetObject("popupMenu"));
            MaximumNumSimulations = new EditView(owner, 
                                                 (Entry)builder.GetObject("maxNumSimulationsEdit"));
            NumberSimulationsLabel = new LabelView(owner, 
                                                   (Label)builder.GetObject("numberOfSimulationsLabel"));
            RunAPSIMAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("runMenuItem"));
            EnableAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("enableMenuItem"));
            DisableAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("disableMenuItem"));
            ExportToCSVAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("exportToCSVMenuItem"));
            ImportFromCSVAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("importFromCSVMenuItem"));
        }

        /// <summary>Grid for holding data.</summary>
        public IListView List { get; }
        
        /// <summary>Gets or sets the value displayed in the number of simulations label./// </summary>
        public ILabelView NumberSimulationsLabel { get; set; }

        /// <summary>Filename textbox.</summary>
        public IEditView MaximumNumSimulations { get; }

        /// <summary>Run APSIM menu item.</summary>
        public IMenuItemView RunAPSIMAction { get; }

        /// <summary>Enable menu item.</summary>
        public IMenuItemView EnableAction { get; }

        /// <summary>Disable menu item.</summary>
        public IMenuItemView DisableAction { get; }

        /// <summary>Generate CSV menu item.</summary>
        public IMenuItemView ExportToCSVAction { get; }

        /// <summary>Import factors menu item.</summary>
        public IMenuItemView ImportFromCSVAction { get; }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                (List as ListView).MainWidget.Cleanup();
                (NumberSimulationsLabel as LabelView).MainWidget.Cleanup();
                (MaximumNumSimulations as EditView).MainWidget.Cleanup();
                (RunAPSIMAction as MenuItemView).Destroy();

                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}