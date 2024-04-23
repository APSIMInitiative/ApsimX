using System;
using System.Collections.Generic;
using UserInterface.Classes;
using UserInterface.EventArguments;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// An interface for a property view.
    /// </summary>
    public interface IPropertyView
    {
        /// <summary>
        /// Display properties to be editable by the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        void DisplayProperties(PropertyGroup properties);

        /// <summary>
        /// Called when a property is changed by the user.
        /// </summary>
        event EventHandler<PropertyChangedEventArgs> PropertyChanged;

        /// <summary>
        /// Fire off a PropertyChanged event for any outstanding changes.
        /// </summary>
        void SaveChanges();
    }
}