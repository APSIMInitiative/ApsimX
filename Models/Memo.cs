using System;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>This is a memo/text component that stores user entered text information.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.HTMLView")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    public class Memo : Model
    {
        /// <summary>Gets or sets the memo text.</summary>
        [XmlIgnore]
        [Description("Text of the memo")]
        public string MemoText { get; set; }

        /// <summary>Gets or sets the code c data.</summary>
        [XmlElement("MemoText")]
        public XmlNode CodeCData
        {
            get
            {
                XmlDocument dummy = new XmlDocument();
                return dummy.CreateCDataSection(MemoText);
            }
            set
            {
                if (value != null)
                {
                    MemoText = value.InnerText;
                }
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (!Name.Equals("TitlePage", StringComparison.CurrentCultureIgnoreCase) || headingLevel == 1)
                tags.Add(new AutoDocumentation.Paragraph(MemoText, indent));
        }


    }
}
