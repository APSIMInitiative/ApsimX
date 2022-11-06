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

        /// <summary>
        /// The word for which we have generated completion options.
        /// </summary>
        public string TriggerWord { get; set; }

        /// <summary>
        /// True iff the selected item is a method.
        /// </summary>
        public bool IsMethod { get; set; }
    }
}
