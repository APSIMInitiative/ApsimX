// ----------------------------------------------------------------------
// <copyright file="SigmoidFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Ymax * 1/1+exp(-(x-Xo)/b)")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SigmoidFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The ymax</summary>
        [ChildLinkByName]
        private IFunction Ymax = null;

        /// <summary>The x value</summary>
        [ChildLinkByName]
        private IFunction XValue = null;

        /// <summary>The Xo</summary>
        [ChildLinkByName]
        private IFunction Xo = null;

        /// <summary>The b</summary>
        [ChildLinkByName]
        private IFunction b = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            try
            {
                double returnValue = Ymax.Value() * 1 / (1 + Math.Exp(-(XValue.Value() - Xo.Value()) / b.Value()));
                Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue);
                return new double[] { returnValue };
            }
            catch (Exception)
            {
                throw new Exception("Error with values to Sigmoid function");
            }
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
                Name = this.Name;
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                tags.Add(new AutoDocumentation.Paragraph(" a sigmoid function of the form " +
                                                         "y = Xmax * 1 / 1 + e<sup>-(XValue - Xo) / b</sup>", indent));

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, 0, indent+1);
            }
        }
    }
}
