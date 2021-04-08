namespace Models
{
    using Models.Core;
    using System;
    using APSIM.Services.Documentation;
    using System.Collections.Generic;

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
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation && Text != null)
                AutoDocumentation.ParseTextForTags(Text, this, tags, headingLevel, indent, true, true);
        }
    }
}
