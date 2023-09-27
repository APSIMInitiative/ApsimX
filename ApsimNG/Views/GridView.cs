using Gtk;
using System;

namespace UserInterface.Views
{

    /// <summary>A view for Table Presenter, can show up to two tables on one screen.</summary>
    public class GridView : ViewBase, IGridView
    {
        /// <summary>Top grid in view.</summary>
        public ContainerView Grid1 { get; private set; }

        /// <summary>Bottom grid in view.</summary>
        public ContainerView Grid2 { get; private set; }

        /// <summary>Label at top of window</summary>
        private Gtk.Label label1;

        /// <summary>Label at top of window</summary>
        private Gtk.VPaned vpaned1;

        /// <summary>Label at top of window</summary>
        private Gtk.VPaned vpaned2;

        /// <summary>Constructor</summary>
        public GridView(ViewBase owner) : base(owner)
        {
            Grid1 = new ContainerView(owner);
            Grid2 = new ContainerView(owner);

            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");

            label1 = (Gtk.Label)builder.GetObject("label1");
            label1.LineWrap = true;

            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            this.SetLabelHeight(0.1f); 

            vpaned2 = (VPaned)builder.GetObject("vpaned2");
            vpaned2.Pack1(Grid1.MainWidget, true, true);
            vpaned2.Pack2(Grid2.MainWidget, true, true);

            //hide all except first grid
            ShowGrid(1, false);
            ShowGrid(2, false);

            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>Hide or Show the Grids</summary>
        public void ShowGrid(int number, bool show)
        {
            if (number == 1)
                Grid1.MainWidget.Visible = show;
            else if (number == 2)
                Grid2.MainWidget.Visible = show;

            if (Grid1.MainWidget.Visible && !Grid2.MainWidget.Visible)
                vpaned2.Position = this.owner.MainWidget.AllocatedHeight;
            else if (!Grid1.MainWidget.Visible && Grid2.MainWidget.Visible)
                vpaned2.Position = 0;
            else
                vpaned2.Position = (int)Math.Round(this.owner.MainWidget.AllocatedHeight * 0.5);
        }

        /// <summary></summary>
        /// <param name="text"></param>
        public void SetLabelText(string text)
        {
            label1.Markup = text;
        }

        /// <summary></summary>
        /// <param name="percentage"></param>
        public void SetLabelHeight(float percentage)
        {
            vpaned1.Position = (int)Math.Round(this.owner.MainWidget.AllocatedHeight * percentage);
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
    public interface IGridView
    {
        /// <summary>Top grid in view.</summary>
        ContainerView Grid1 { get; }

        /// <summary>bottom grid in view.</summary>
        ContainerView Grid2 { get; }

        /// <summary>Show the 2nd grid?</summary>
        void ShowGrid(int number, bool show);

        /// <summary></summary>
        public void SetLabelText(string text);

        /// <summary></summary>
        public void SetLabelHeight(float percentage);
    }
}
