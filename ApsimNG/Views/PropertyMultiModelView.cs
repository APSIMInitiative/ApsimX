namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using Classes;
    using Gtk;
    using Utility;
    using System.Linq;
    using Models.Core;
    using EventArguments;
    using APSIM.Shared.Utilities;
    using System.Globalization;
    using System.Reflection;
    using Extensions;

    /// <summary>
    /// This class inherits the PropertyView and overrides the methods needed to display a list of models (children)
    /// as columns in the Property table. 
    /// </summary>
    /// <remarks>
    /// An additional row header with the model names is added.
    /// </remarks>
    public class PropertyMultiModelView : PropertyView
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public PropertyMultiModelView(ViewBase owner) : base(owner)
        {
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public override void DisplayProperties(PropertyGroup properties)
        {
            throw new NotImplementedException("Single models is not supported in PropertyMultiModelView");
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public void DisplayProperties(List<PropertyGroup> properties)
        {

            int row = 0;
            int col = 0;

            bool widgetIsFocused = false;
            int entryPos = -1;
            int entrySelectionStart = 0;
            int entrySelectionEnd = 0;


            box.Remove(propertyTable);

            propertyTable.Dispose();


            propertyTable = new Grid();
            //propertyTable.RowHomogeneous = true;
            propertyTable.RowSpacing = 5;

            propertyTable.Destroyed += PropertyTable_Destroyed;
            box.Add(propertyTable);


            int nrow = 0;

            // for each model in list to display as columns. 
            for (int i = 0; i < properties.Count; i++)
            {
                Label label = new Label(properties[i].Name);
                label.TooltipText = $"Multiple entry #{i+1}";
                label.Xalign = 0;

                label.MarginEnd = 10;
                propertyTable.Attach(label, 2+i, 0, 1, 1);
                nrow = 1;
                AddPropertiesToTable(ref propertyTable, properties[i], ref nrow, i);

            }

            mainWidget.ShowAll();

            // If a widget was previously focused, then try to give it focus again.
            if (widgetIsFocused)
            {
                Widget widget = propertyTable.GetChildAt(row, col);
                if (widget is Entry entry)
                {
                    entry.GrabFocus();
                    if (entrySelectionStart >= 0 && entrySelectionStart < entrySelectionEnd && entrySelectionEnd <= entry.Text.Length)
                        entry.SelectRegion(entrySelectionStart, entrySelectionEnd);
                    else if (entryPos > -1 && entry.Text.Length >= entryPos)
                        entry.Position = entryPos;
                }
            }
        }
    }
}