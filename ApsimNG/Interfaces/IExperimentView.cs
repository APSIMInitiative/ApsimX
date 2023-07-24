namespace UserInterface.Interfaces
{
    using global::UserInterface.Views;

    interface IExperimentView
    {
        /// <summary>Grid for holding data.</summary>
        IListView List { get; }

        /// <summary>Gets or sets the value displayed in the number of simulations label./// </summary>
        ILabelView NumberSimulationsLabel { get; }

        /// <summary>Filename textbox.</summary>
        IEditView MaximumNumSimulations { get; }

        /// <summary>Enable menu item.</summary>
        IMenuItemView EnableAction { get; }

        /// <summary>Disable menu item.</summary>
        IMenuItemView DisableAction { get; }

        /// <summary>Generate CSV menu item.</summary>
        IMenuItemView ExportToCSVAction { get; }

        /// <summary>Import factors menu item.</summary>
        IMenuItemView ImportFromCSVAction { get; }
        
        /// <summary>Run APSIM menu item.</summary>
        IMenuItemView RunAPSIMAction { get; }

        /// <summary>Add a menu item to the popup menu</summary>
        /// <returns>Reference to the menuItemView to attach events</returns>
        IMenuItemView AddMenuItem(string label);
    }
}
