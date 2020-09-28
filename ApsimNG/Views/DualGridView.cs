using Gtk;
using System;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    /// <summary>An interface for a drop down</summary>
    public interface IDualGridView
    {
        /// <summary>Top grid in view.</summary>
        IGridView Grid1 { get; }

        /// <summary>bottom grid in view.</summary>
        IGridView Grid2 { get; }
    }

    /// <summary>A drop down view.</summary>
    public class DualGridView : ViewBase, IDualGridView
    {
        /// <summary>Top grid in view.</summary>
        public IGridView Grid1 { get; private set; }

        /// <summary>bottom grid in view.</summary>
        public IGridView Grid2 { get; private set; }

        /// <summary>Constructor</summary>
        public DualGridView(ViewBase owner) : base(owner)
        {
            Grid1 = new GridView(owner);
            Grid2 = new GridView(owner);

            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.DualGridView.glade");
            VPaned vpaned1 = (VPaned)builder.GetObject("vpaned1");
            VPaned vpaned2 = (VPaned)builder.GetObject("vpaned2");
            VBox vbox1 = (VBox)builder.GetObject("vbox1");
            mainWidget = vpaned1;
            vbox1.PackStart((Grid1 as GridView).MainWidget, true, true, 0);
            vpaned2.Pack1((Grid2 as GridView).MainWidget, true, true);
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
}
