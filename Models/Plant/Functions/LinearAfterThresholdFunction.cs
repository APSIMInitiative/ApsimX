// -----------------------------------------------------------------------
// <copyright file="LinearAfterThresholdFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Function to return the value for a function with a trigger and slope from it
    ///                /
    ///               /
    /// -------------/
    ///             x
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Use a linear function with a gradient after a trigger value is exceeded.")]
    public class LinearAfterThresholdFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>A locator object</summary>
        [Link]
        private ILocator locator = null;

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
        public override double[] Values()
        {
            object v = locator.Get(XProperty);
            if (v == null)
                throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
            if (v is IFunction)
                v = (v as IFunction).Values();

            if (v is double[])
            {
                double[] array = v as double[];
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] <= XTrigger)
                        array[i] = 0;
                    else
                        array[i] = Math.Max(0.0, (array[i] - XTrigger)) * Slope;
                }
                return array;
            }
            else if (v is double)
            {
                double xValue = (double)v;
                if (xValue <= XTrigger)
                    return new double[] { 0 };
                else
                    return new double[] { Math.Max(0.0, (xValue - XTrigger)) * Slope };
            }
            else
                throw new Exception("Cannot do a linear interpolation on type: " + v.GetType().Name +
                                    " in function: " + Name);
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
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

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
