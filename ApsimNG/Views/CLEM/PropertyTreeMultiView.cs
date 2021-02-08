namespace UserInterface.Views
{
    using EventArguments;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// GTK# based view of the PropertyTreePresenter to display a tree view of categories and sub-categories to assit filtering properties
    /// This class inherits the PropertyTreePresenter which will provide all child models of the same type as columns of the property table for the user to update
    /// This could be used to display all soil layers models contained below another model or folder as columns and provide all properties
    /// Uses Category attribute of property (Category and SubCategory values) to define list and modify SimplePropertyPresenter filter rule on selection
    /// A right hand panel is used to display the property presenter
    /// </remarks>
    public class PropertyTreeMultiView : PropertyTreeView
    {
        public PropertyTreeMultiView(ViewBase owner) : base(owner)
        {
        }
    }
}
