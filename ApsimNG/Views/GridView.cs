using Gtk;
using System;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{

    /// <summary>A view for Table Presenter, can show up to two tables on one screen.</summary>
    public class GridView : ViewBase, IGridView
    {
        /// <summary>Top grid in view.</summary>
        public ContainerView Grid1 { get; private set; }

        /// <summary>Bottom grid in view.</summary>
        public ContainerView Grid2 { get; private set; }

        /// <summary>Right-Top grid in view.</summary>
        public ContainerView Grid3 { get; private set; }

        /// <summary>Right-Bottom grid in view.</summary>
        public ContainerView Grid4 { get; private set; }

        /// <summary>Label at top of window</summary>
        private Gtk.Label descriptionLabel;

        /// <summary></summary>
        private Gtk.Label grid1Label;

        /// <summary></summary>
        private Gtk.Label grid2Label;

        /// <summary></summary>
        private Gtk.Label grid3Label;

        /// <summary></summary>
        private Gtk.Label grid4Label;

        /// <summary>Label at top of window</summary>
        private Gtk.Paned vpaned1;

        /// <summary>Holder of the 4 tables</summary>
        private Gtk.Paned vpaned2;

        /// <summary>Label at top of window</summary>
        private Gtk.Paned vpaned3;

        /// <summary>Label at top of window</summary>
        private Gtk.Paned vpaned4;

        /// <summary>Constructor</summary>
        public GridView(ViewBase owner) : base(owner)
        {
            Grid1 = new ContainerView(owner);
            Grid2 = new ContainerView(owner);
            Grid3 = new ContainerView(owner);
            Grid4 = new ContainerView(owner);

            Box Grid1Box = new Box(Orientation.Vertical, 0);
            grid1Label = new Label();
            grid1Label.Markup = "";
            Grid1Box.PackStart(grid1Label, false, false, 0);
            Grid1Box.PackEnd(Grid1.MainWidget, true, true, 0);

            Box Grid2Box = new Box(Orientation.Vertical, 0);
            grid2Label = new Label();
            grid2Label.Markup = "";
            Grid2Box.PackStart(grid2Label, false, false, 0);
            Grid2Box.PackEnd(Grid2.MainWidget, true, true, 0);

            Box Grid3Box = new Box(Orientation.Vertical, 0);
            grid3Label = new Label();
            grid3Label.Markup = "";
            Grid3Box.PackStart(grid3Label, false, false, 0);
            Grid3Box.PackEnd(Grid3.MainWidget, true, true, 0);

            Box Grid4Box = new Box(Orientation.Vertical, 0);
            grid4Label = new Label();
            grid4Label.Markup = "";
            Grid4Box.PackStart(grid4Label, false, false, 0);
            Grid4Box.PackEnd(Grid4.MainWidget, true, true, 0);

            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");

            descriptionLabel = (Gtk.Label)builder.GetObject("label1");
            descriptionLabel.LineWrap = true;

            vpaned1 = (Paned)builder.GetObject("vpaned1");
            this.SetLabelHeight(0.1f);

            vpaned2 = (Paned)builder.GetObject("vpaned2");

            vpaned3 = (Paned)builder.GetObject("vpaned3");
            vpaned3.Pack1(Grid1Box, true, true);
            vpaned3.Pack2(Grid2Box, true, true);

            vpaned4 = (Paned)builder.GetObject("vpaned4");
            vpaned4.Pack1(Grid3Box, true, true);
            vpaned4.Pack2(Grid4Box, true, true);

            //hide all except first grid
            
            while (!(owner is ExplorerView) && owner.Owner != null)
                owner = owner.Owner;
            if (owner != null)
            {
                ShowGrid(1, false, owner as ExplorerView);
                ShowGrid(2, false, owner as ExplorerView);
                ShowGrid(3, false, owner as ExplorerView);
                ShowGrid(4, false, owner as ExplorerView);
            }
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>Hide or Show the Grids</summary>
        public void ShowGrid(int number, bool show, ExplorerView ev)
        {
            System.Drawing.Rectangle bounds = GtkUtilities.GetBorderOfRightHandView(ev);

            if (number == 1)
                Grid1.MainWidget.Visible = show;
            else if (number == 2)
                Grid2.MainWidget.Visible = show;
            else if (number == 3)
                Grid3.MainWidget.Visible = show;
            else if (number == 4)
                Grid4.MainWidget.Visible = show;

            if (Grid3.MainWidget.Visible || Grid4.MainWidget.Visible)
                vpaned2.Position = bounds.Width / 2;
            else
                vpaned2.Position = bounds.Width;

            if (Grid1.MainWidget.Visible && !Grid2.MainWidget.Visible)
                vpaned3.Position = bounds.Height;
            else if (!Grid1.MainWidget.Visible && Grid2.MainWidget.Visible)
                vpaned3.Position = 0;
            else
                vpaned3.Position = bounds.Height / 2;

            if (Grid3.MainWidget.Visible && !Grid4.MainWidget.Visible)
                vpaned4.Position = bounds.Height;
            else if (!Grid3.MainWidget.Visible && Grid4.MainWidget.Visible)
                vpaned4.Position = 0;
            else
                vpaned4.Position = bounds.Height / 2;
        }

        /// <summary></summary>
        /// <param name="text"></param>
        public void SetDescriptionText(string text)
        {
            descriptionLabel.Markup = text;
        }

        /// <summary></summary>
        /// <param name="text"></param>
        /// <param name="table"></param>
        public void SetTableLabelText(string text, int table)
        {
            if (table == 1)
                grid1Label.Markup = text;
            else if (table == 2)
                grid2Label.Markup = text;
            else if (table == 3)
                grid3Label.Markup = text;
            else if (table == 4)
                grid4Label.Markup = text;
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

        /// <summary>Top grid in view.</summary>
        ContainerView Grid3 { get; }

        /// <summary>bottom grid in view.</summary>
        ContainerView Grid4 { get; }

        /// <summary>Show the 2nd grid?</summary>
        void ShowGrid(int number, bool show, ExplorerView ev);

        /// <summary>Sets the text displayed at the top of the screen.</summary>
        public void SetDescriptionText(string text);

        /// <summary>Sets the text displayed above each table</summary>
        /// <param name="text"></param>
        /// <param name="table"></param>
        public void SetTableLabelText(string text, int table);

        /// <summary></summary>
        public void SetLabelHeight(float percentage);
    }
}
