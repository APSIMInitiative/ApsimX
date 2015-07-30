using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A constant value function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Constant : Model, IFunction
    {
        /// <summary>Gets the value.</summary>
        [Description("The value of the constant")]
        public double Value { get; set; }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            if (headingLevel > 0)
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // get description and units.
            string description = AutoDocumentation.GetDescription(Parent, Name);
            string units = AutoDocumentation.GetUnits(Parent, Name);
            if (units != string.Empty)
                units = " (" + units + ")";

            if (description != string.Empty)
                tags.Add(new AutoDocumentation.Paragraph(description, indent));
            tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + Value + units + "</i>.", indent));
        }
    }
}