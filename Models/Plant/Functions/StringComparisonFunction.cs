using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    [Description("Tests if value of the first child is less than value of second child. Returns third child if true and forth if false")]
    public class StringComparisonFunction : Model, IFunction
    {

        /// <summary>The propertyname</summary>
        [Description("Name of string property to compare")]
        public string PropertyName { get; set; }

        /// <summary>The string value</summary>
        [Description("Text string for comparison to the property value")]
        public string StringValue { get; set; }

        /// <summary>The True Value</summary>
        [Link]
        IFunction TrueValue = null;

        /// <summary>The False Value</summary>
        [Link]
        IFunction FalseValue = null;

        [Link]
        private ILocator locator = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            object s = locator.Get(PropertyName);
            if (s == null)
                throw new Exception("Cannot find value for " + Name + " PropertyName: " + PropertyName);

            string PropertyString;
            if (s is Array)
                PropertyString = (string)(s as Array).GetValue(arrayIndex);
            else if (s is IFunction)
                PropertyString = (s as IFunction).Value(arrayIndex).ToString();
            else
                PropertyString = (string)s;

            bool stringCompareTrue = PropertyString.Equals(StringValue, StringComparison.CurrentCultureIgnoreCase);

            if (stringCompareTrue)
                return TrueValue.Value(arrayIndex);
            else
                return FalseValue.Value(arrayIndex);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {

                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                tags.Add(new AutoDocumentation.Paragraph("If " + PropertyName + " = " + StringValue + " Then", indent));
                (TrueValue as IModel).Document(tags, headingLevel, indent + 1);
                tags.Add(new AutoDocumentation.Paragraph("Else", indent));
                (FalseValue as IModel).Document(tags, headingLevel, indent + 1);
            }
        }
    }
}