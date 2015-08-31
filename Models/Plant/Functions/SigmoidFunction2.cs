using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SigmoidFunction2 : Model, IFunction
    {
        /// <summary>The ymax</summary>
        [Link]
        IFunction Ymax = null;
        /// <summary>The x value</summary>
        [Link]
        IFunction XValue = null;
        /// <summary>The Xo</summary>
        [Link]
        IFunction Xo = null;
        /// <summary>The b</summary>
        [Link]
        IFunction b = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Sigmoid function</exception>
        public double Value
        {
            get
            {

                try
                {
                    double _return = Ymax.Value * 1 / (1 + Math.Exp(-(XValue.Value - Xo.Value) / b.Value));
                    return _return;
                }
                catch (Exception)
                {
                    throw new Exception("Error with values to Sigmoid function");
                }
            }
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            Name = this.Name;
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            tags.Add(new AutoDocumentation.Paragraph(" a sigmoid function of the form " +
                                                      "y = Xmax * 1 / 1 + e<sup>-(XValue - Xo) / b</sup>", indent));


            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                    child.Document(tags, headingLevel + 1, indent+1);
            }
        }
    }
}
