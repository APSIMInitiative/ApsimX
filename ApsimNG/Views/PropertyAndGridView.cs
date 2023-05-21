using Gtk;
using System;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{

    /// <summary>A drop down view.</summary>
    public class PropertyAndGridView : ViewBase, IPropertyAndGridView
    {
        /// <summary>Top grid in view.</summary>
        public ViewBase PropertiesView { get; private set; }

        /// <summary>bottom grid in view.</summary>
        public IGridView Grid2 { get; private set; }

        /// <summary>Constructor</summary>
        public PropertyAndGridView(ViewBase owner) : base(owner)
        {
            PropertiesView = new PropertyView(owner);
            Grid2 = new GridView(owner);

            VPaned panel = new VPaned();
            mainWidget = panel;
            panel.Pack1((PropertiesView as ViewBase).MainWidget, true, false);
            panel.Pack2((Grid2 as ViewBase).MainWidget, true, false);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>An interface for a composite view which shows a property view and a grid view.</summary>
    public interface IPropertyAndGridView
    {
        /// <summary>Top grid in view.</summary>
        ViewBase PropertiesView { get; }

        /// <summary>bottom grid in view.</summary>
        IGridView Grid2 { get; }
    }
}
