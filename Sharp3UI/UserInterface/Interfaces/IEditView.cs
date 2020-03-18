using System;
using System.Collections.Generic;
using System.Text;

namespace UserInterface.Interfaces
{
    /// <summary>An interface for a drop down</summary>
    public interface IEditView
    {
        /// <summary>Invoked when the edit box loses focus.</summary>
        event EventHandler Leave;

        /// <summary>Invoked when the user changes the text in the edit box.</summary>
        event EventHandler Changed;

        ///// <summary>
        ///// Invoked when the user needs intellisense items.
        ///// Currently this is only triggered by pressing control-space.
        ///// </summary>
        //event EventHandler<NeedContextItemsArgs> IntellisenseItemsNeeded;

        /// <summary>Gets or sets the Text</summary>
        string Value { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets the offset of the cursor in the textbox.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Inserts the selected text at the cursor, replacing all text
        /// before the cursor and after the most recent character which
        /// is not an opening square bracket.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        void InsertAtCursorInSquareBrackets(string text);

        /// <summary>
        /// Insert text at the cursor.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        void InsertAtCursor(string text);

        /// <summary>
        /// Inserts a completion option, replacing the half-typed trigger word
        /// for which we have generated completion options.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        /// <param name="triggerWord">Incomplete word to be replaced.</param>
        void InsertCompletionOption(string text, string triggerWord);
    }
}
