using Gtk;
using System;

namespace Gtk.Sheet
{
    /// <summary>Describes the public interface of a class that supports editing sheet cells.</summary>
    public interface ISheetEditor
    {
        //event EventHandler<NeedContextItemsArgs> ShowIntellisense;

        /// <summary>Returns true if the editor is currently editing a cell.</summary>
        bool IsEditing { get; }

        /// <summary>Display an entry box for the user to edit the current selected cell data.</summary>
        void Edit(char defaultChar = char.MinValue);

        /// <summary>End edit more.</summary>
        void EndEdit(bool saveEdit = true);
    }
}