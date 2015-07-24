// -----------------------------------------------------------------------
// <copyright file="FolderView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Describes an interface for a folder view.
    /// </summary>
    interface IFolderView
    {
        /// <summary>Sets the user controls to show.</summary>
        void SetContols(List<UserControl> controls);
    }

    /// <summary>
    /// A view for showing 1 or more user controls.
    /// </summary>
    public partial class FolderView : UserControl, IFolderView
    {
        /// <summary>Sets the controls to show.</summary>
        public void SetContols(List<UserControl> controls)
        {
            foreach (UserControl control in controls)
                control.Parent = this;

            PositionAndRefreshControls();
        }

        /// <summary>Positions and refreshes all controls.</summary>
        private void PositionAndRefreshControls()
        {
            this.Resize -= OnResize;
            int numControls = Controls.Count;
            if (numControls > 0)
            {

                int numRows;
                if (numControls == 1)
                    numRows = 1;
                else
                    numRows = (int)Math.Sqrt(numControls - 1) + 1;
                int numCols = (int)Math.Ceiling((double)numControls / numRows);
                int width = Size.Width / numCols;
                int height = Size.Height / numRows - 1;
                int controlNumber = 0;
                int col = 0;
                int row = 0;
                foreach (Control control in Controls)
                {
                    control.Location = new Point(col * width, row * height);
                    control.Width = width;
                    control.Height = height;
                    controlNumber++;
                    col++;
                    if (col >= numCols)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
            this.Resize += OnResize;
        }

        /// <summary>Called when user resized view.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnResize(object sender, EventArgs e)
        {
            PositionAndRefreshControls();
        }
    }
}
