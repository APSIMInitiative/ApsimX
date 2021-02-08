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

    /// <summary>
    /// This class inherits the PropertyView and overrides the methods needed to display a list of models (children)
    /// as columns in the table of the Property table. 
    /// An additional row header with the model names is added.
    /// </remarks>
    public class PropertyMultiView : PropertyView
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public PropertyMultiView(ViewBase owner) : base(owner)
        {
            // Columns should not be homogenous - otherwise we'll have the
            // property name column taking up half the screen.
            propertyTable = new Table(0, 0, false);
            box = new Frame("Properties");
            box.Add(propertyTable);
            mainWidget = box;
            mainWidget.Destroyed += OnWidgetDestroyed;
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public override void DisplayProperties(PropertyGroup properties)
        {
            throw new NotImplementedException("Single models is not supported in PropertyMultiView");
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public override void DisplayProperties(List<PropertyGroup> properties)
        {
            uint row = 0;
            uint col = 0;
            bool widgetIsFocused = false;
            int entryPos = -1;
            int entrySelectionStart = 0;
            int entrySelectionEnd = 0;
            if (propertyTable.FocusChild != null)
            {
                object topAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "top-attach").Val;
                object leftAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "left-attach").Val;
                if (topAttach.GetType() == typeof(uint) && leftAttach.GetType() == typeof(uint))
                {
                    row = (uint)topAttach;
                    col = (uint)leftAttach;
                    widgetIsFocused = true;
                    if (propertyTable.FocusChild is Entry entry)
                    {
                        entryPos = entry.Position;
                        entry.GetSelectionBounds(out entrySelectionStart, out entrySelectionEnd);
                    }
                }
            }

            if (DisplayFrame)
            {
                box.Label = $"{properties.FirstOrDefault().Name} Properties";
            }
            else
            {
                box.ShadowType = ShadowType.None;
                box.Label = null;
            }
            propertyTable.Destroy();

            //// Columns should not be homogenous - otherwise we'll have the
            //// property name column taking up half the screen.
            propertyTable = new Table((uint)properties.Count()+1, (uint)(2 + properties.Count()), false);
            
            // column and row spacing 
            propertyTable.RowSpacing = 3;
            propertyTable.ColumnSpacing = 3;

            propertyTable.Destroyed += OnWidgetDestroyed;
            box.Add(propertyTable);

            uint nrow = 0;
            for (int i = 0; i < properties.Count; i++)
            {
                Label label = new Label(properties[i].Name);
                label.TooltipText = $"Multiple entry #{i+1}";
                label.Xalign = 0;
                propertyTable.Attach(label, 2+(uint)i, 3 + (uint)i, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 5, 0);
                nrow = 1;
                AddPropertiesToTable(ref propertyTable, properties[i], ref nrow, (uint)i);
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