using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// Function to return the value for a function with a trigger and slope from it
    ///                /
    ///               /
    /// -------------/
    ///             x
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.LinearAfterThresholdPresenter")]
    [Description("Use a linear function with a gradient after a trigger value is exceeded.")]
    public class LinearAfterThresholdFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }
        
        /// <summary>
        /// The trigger value on the X axis
        /// </summary>
        [Description("X value trigger")]
        public double XTrigger { get; set; }

        /// <summary>
        /// The slope or gradient of the linear series
        /// </summary>
        [Description("Gradient")]
        public double Slope { get; set; }

        /// <summary>Constructor</summary>
        public LinearAfterThresholdFunction() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xproperty"></param>
        public LinearAfterThresholdFunction(string xproperty)
        {
            XProperty = xproperty;
        }

        /// <summary>
        /// Get the value of the function
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            object v = this.FindByPath(XProperty)?.Value;
            if (v == null)
                throw new Exception($"Cannot find value for {FullPath} XProperty: {XProperty}");
            double x;
            if (v is Array)
                x = (double)(v as Array).GetValue(arrayIndex);
            else if (v is IFunction)
                x = (v as IFunction).Value(arrayIndex);
            else
                x = (double)v;

            return ValueForX(x);
        }

        /// <summary>
        /// Gets the value of the function for a given value of the x property.
        /// </summary>
        /// <param name="x">An x-value.</param>
        public double ValueForX(double x)
        {
            if (x <= XTrigger)
                return 0;
            else
                return Math.Max(0.0, (x - XTrigger)) * Slope;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // get description and units.
                string description = AutoDocumentation.GetDescription(Parent, Name);

                if (description != string.Empty)
                    tags.Add(new AutoDocumentation.Paragraph(description, indent));
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated as a function of <i>" + StringUtilities.RemoveTrailingString(XProperty, ".Value()") + "</i>", indent));
                tags.Add(new AutoDocumentation.Paragraph("<i>Trigger value" + XTrigger.ToString() + " Gradient " + Slope.ToString() + "</i>", indent));
            }
        }
    }
}
