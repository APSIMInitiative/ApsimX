using Models.Core;
using System;
using APSIM.Shared.Documentation;
using System.Collections.Generic;

namespace Models
{
    /// <summary>This is a memo/text component that stores user entered text information.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.MemoView.glade")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Memo : Model
    {
        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the memo")]
        public string Text { get; set; }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph(Text);
        }
    }
}
