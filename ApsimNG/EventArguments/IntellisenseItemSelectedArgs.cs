using System;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// Event arguments used when inserting an intellisense item into a grid or text editor.
    /// </summary>
    public class IntellisenseItemSelectedArgs : EventArgs
    {
        /// <summary>
        /// Text which is to be inserted.
        /// </summary>
        public string ItemSelected { get; set; }
    }
}
