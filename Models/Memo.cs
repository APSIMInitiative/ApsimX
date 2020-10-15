namespace Models
{
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>This is a memo/text component that stores user entered text information.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.MemoView.glade")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Memo : Model, ICustomDocumentation
    {
        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the memo")]
        public string Text { get; set; }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation && Text != null)
                AutoDocumentation.ParseTextForTags(Text, this, tags, headingLevel, indent, true, true);
        }
    }
}
