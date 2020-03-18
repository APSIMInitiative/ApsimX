using System;
using System.Collections.Generic;
using System.Text;

namespace UserInterface.Interfaces
{
    /// <summary>An interface for a list box</summary>
    public interface IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        event EventHandler DoubleClicked;

        /// <summary>Get or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// If true, we are display a list of models
        /// This will turn on display of images and drag-drop logic
        /// </summary>
        bool IsModelList { get; set; }

        /// <summary>
        /// Populates a context menu
        /// </summary>
        /// <param name="menuDescriptions"></param>
        void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions);

        /// <summary>
        /// Invoked when a drag operation has commenced. Need to create a DragObject.
        /// </summary>
        event EventHandler<DragStartArgs> DragStarted;
    }
}
