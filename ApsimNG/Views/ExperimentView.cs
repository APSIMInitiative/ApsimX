using UserInterface.Interfaces;
using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserInterface.Views
{
    public class ExperimentView : ViewBase, IExperimentView
    {
        private Gtk.Menu menu;

        public ExperimentView() { }

        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public ExperimentView(ViewBase owner) : base(owner)
        {
            Initialise(owner, null);
        }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            base.Initialise(ownerView, gtkControl);
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ExperimentView.glade");

            Box box = (Box)builder.GetObject("vbox");
            if (gtkControl is Container container)
            {
                container.Add(box);
                mainWidget = container;
            }
            else
            {
                mainWidget = box;
            }
            InitialiseWidget(builder);
        }

        private void InitialiseWidget(Builder builder)
        {
            mainWidget.Destroyed += OnMainWidgetDestroyed;
            menu = (Gtk.Menu)builder.GetObject("popupMenu");
            List = new ListView(owner, 
                                (Gtk.TreeView) builder.GetObject("list"),
                                menu);
            MaximumNumSimulations = new EditView(owner, 
                                                 (Entry)builder.GetObject("maxNumSimulationsEdit"));
            NumberSimulationsLabel = new LabelView(owner, 
                                                   (Label)builder.GetObject("numberOfSimulationsLabel"));
            RunAPSIMAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("runMenuItem"));
            EnableAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("enableMenuItem"));
            DisableAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("disableMenuItem"));
            ExportToCSVAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("exportToCSVMenuItem"));
            ImportFromCSVAction = new MenuItemView((Gtk.MenuItem)builder.GetObject("importFromCSVMenuItem"));
            PlaylistAction = new List<IMenuItemView>();
        }

        /// <summary>Grid for holding data.</summary>
        public IListView List { get; private set; }
        
        /// <summary>Gets or sets the value displayed in the number of simulations label</summary>
        public ILabelView NumberSimulationsLabel { get; set; }

        /// <summary>Maximum number of simulations to show</summary>
        public IEditView MaximumNumSimulations { get; private set; }

        /// <summary>Run APSIM menu item.</summary>
        public IMenuItemView RunAPSIMAction { get; private set; }

        /// <summary>Enable menu item.</summary>
        public IMenuItemView EnableAction { get; private set; }

        /// <summary>Disable menu item.</summary>
        public IMenuItemView DisableAction { get; private set; }

        /// <summary>Generate CSV menu item.</summary>
        public IMenuItemView ExportToCSVAction { get; private set; }

        /// <summary>Import factors menu item.</summary>
        public IMenuItemView ImportFromCSVAction { get; private set; }

        /// <summary>Adds Simulations to Playlist</summary>
        public List<IMenuItemView> PlaylistAction { get; private set; }

        /// <summary>Add a menu item to the popup menu</summary>
        /// <returns>Reference to the menuItemView to attach events</returns>
        public IMenuItemView AddMenuItem(string label)
        {
            Gtk.MenuItem item = new Gtk.MenuItem(label);
            MenuItemView view = new MenuItemView(item);
            PlaylistAction.Append(view);
            menu.Add(item);
            return view;
        }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                (List as ListView).Dispose();
                (NumberSimulationsLabel as LabelView).Dispose();
                (MaximumNumSimulations as EditView).Dispose();
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