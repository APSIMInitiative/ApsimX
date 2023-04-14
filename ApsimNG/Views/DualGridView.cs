using Gtk;
using System;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

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

            VPaned panel = new VPaned();
            mainWidget = panel;
            panel.Pack1((Grid1 as GridView).MainWidget, true, true);
            panel.Pack2((Grid2 as GridView).MainWidget, true, true);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>Show the 2nd grid?</summary>
        public void ShowGrid2(bool show)
        {
            (Grid2 as GridView).MainWidget.Visible = show;
        }


        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                //mainWidget.Dispose();
                Grid1.Dispose();
                Grid2.Dispose();
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>An interface for a drop down</summary>
    public interface IDualGridView
    {
        /// <summary>Top grid in view.</summary>
        IGridView Grid1 { get; }

        /// <summary>bottom grid in view.</summary>
        IGridView Grid2 { get; }

        /// <summary>Show the 2nd grid?</summary>
        void ShowGrid2(bool show);
    }
}
