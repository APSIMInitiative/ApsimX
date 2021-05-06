using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns the value at the given index. If the index is outside the array, the last value will be returned.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ArrayFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>Gets the value.</summary>
        [Description("The values of the array (space seperated)")]
        public string Values { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The optional units of the array")]
        public string Units { get; set; }

        private List<double> str2dbl = new List<double>();

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new ApsimXException(this, "ArrayFunction must have an index to return.");

            if (str2dbl.Count == 0)
            {
                string[] split = Values.Split(' ');
                foreach (string s in split)
                    try
                    {
                        str2dbl.Add(Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch (Exception)
                    {
                        throw new ApsimXException(this, "ArrayFunction: Could not convert " + s + " to a number.");
                    }
            }

            if (arrayIndex > str2dbl.Count - 1)
                return str2dbl[str2dbl.Count - 1];

            return str2dbl[arrayIndex];
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // get description and units.
            string description = AutoDocumentation.GetDescription(Parent, Name);
            string units = Units;
            if (units == null)
                units = AutoDocumentation.GetUnits(Parent, Name);
            if (units != string.Empty)
                units = " (" + units + ")";

            if (!(Parent is IFunction) && headingLevel > 0)
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            //TODO: Tidy up the printing -JF
            tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + Values + units + "</i>", indent));

            if (!String.IsNullOrEmpty(description))
                tags.Add(new AutoDocumentation.Paragraph(description, indent));

            // write memos.
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

        }
    }
}