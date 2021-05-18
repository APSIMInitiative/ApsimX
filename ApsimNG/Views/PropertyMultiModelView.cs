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
#if NETFRAMEWORK
            uint row = 0;
            uint col = 0;
#else
            int row = 0;
            int col = 0;
#endif
            bool widgetIsFocused = false;
            int entryPos = -1;
            int entrySelectionStart = 0;
            int entrySelectionEnd = 0;
#if NETFRAMEWORK
            if (propertyTable.FocusChild != null)
            {
                object topAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "top-attach").Val;
                object leftAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "left-attach").Val;
                if (topAttach.GetType() == typeof(uint) && leftAttach.GetType() == typeof(uint))
                {
#if NETFRAMEWORK
                    row = (uint)topAttach;
                    col = (uint)leftAttach;
#else
                    row = (int)topAttach;
                    col = (int)leftAttach;
#endif
                    widgetIsFocused = true;
                    if (propertyTable.FocusChild is Entry entry)
                    {
                        entryPos = entry.Position;
                        entry.GetSelectionBounds(out entrySelectionStart, out entrySelectionEnd);
                    }
                }
            }
#endif

            box.Remove(propertyTable);

            propertyTable.Cleanup();

#if NETFRAMEWORK
            // Columns should not be homogenous - otherwise we'll have the
            // property name column taking up half the screen.
            propertyTable = new Table((uint)properties.Count() + 1, (uint)(2 + properties.Count()), false);
            // column and row spacing 
            propertyTable.RowSpacing = 3;
            propertyTable.ColumnSpacing = 3;
#else
            propertyTable = new Grid();
            //propertyTable.RowHomogeneous = true;
            propertyTable.RowSpacing = 5;
#endif
            propertyTable.Destroyed += OnWidgetDestroyed;
            box.Add(propertyTable);

#if NETFRAMEWORK
            uint nrow = 0;
#else
            int nrow = 0;
#endif
            // for each model in list to display as columns. 
            for (int i = 0; i < properties.Count; i++)
            {
                Label label = new Label(properties[i].Name);
                label.TooltipText = $"Multiple entry #{i+1}";
                label.Xalign = 0;
#if NETFRAMEWORK
                propertyTable.Attach(label, 2+(uint)i, 3+(uint)i, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 5, 0);
                nrow = 1;
                AddPropertiesToTable(ref propertyTable, properties[i], ref nrow, (uint)i);
#else
                label.MarginEnd = 10;
                propertyTable.Attach(label, 2+i, 0, 1, 1);
                nrow = 1;
                AddPropertiesToTable(ref propertyTable, properties[i], ref nrow, i);
#endif
            }

            mainWidget.ShowAll();

            // If a widget was previously focused, then try to give it focus again.
            if (widgetIsFocused)
            {
                Widget widget = propertyTable.GetChild(row, col);
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