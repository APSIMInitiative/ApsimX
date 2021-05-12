using System;
using System.Collections.Generic;
using Models.Core;


namespace Models.Functions
{
    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    [Description("Tests if value of a string property is equal to a given value and returns a value depending on the result.")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StringComparisonFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The propertyname</summary>
        [Description("Name of string property to compare")]
        public string PropertyName { get; set; }

        /// <summary>The string value</summary>
        [Description("Text string for comparison to the property value")]
        public string StringValue { get; set; }

        /// <summary>The True Value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction TrueValue = null;

        /// <summary>The False Value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FalseValue = null;

        [Link]
        private ILocator locator = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            object s = locator.Get(PropertyName);

            string PropertyString;
            if (s == null)
                PropertyString = "";
            else if (s is Array)
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
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {

                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Paragraph("If " + PropertyName + " = " + StringValue + " Then", indent));
                AutoDocumentation.DocumentModel(TrueValue as IModel,tags, headingLevel+1, indent+1);

                tags.Add(new AutoDocumentation.Paragraph("Else", indent));
                AutoDocumentation.DocumentModel(FalseValue as IModel, tags, headingLevel+1, indent+1);
            }
        }
    }
}