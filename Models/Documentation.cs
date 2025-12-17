using System;
using Models.Core;

namespace Models
{
    /// <summary>This is a documentation component that stores user entered text information for display in the autodocs system.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.MemoView.glade")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Documentation : Model, IText
    {
        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the memo")]
        public string Text { get; set; }
    }
}