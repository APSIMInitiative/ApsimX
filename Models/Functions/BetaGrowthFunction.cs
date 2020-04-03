using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Functions
{
    /// <summary>
    /// Takes the value of the child as the x value and returns the y value 
    /// from a beta growth function of the form y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))
    /// See Yin et al 2003 A Flexible Sigmoid Function of Determinate Growth
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a beta growth function of the form y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BetaGrowthFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>Ymax, the maximum value for weight, length or others </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Ymax = null;
        /// <summary>The thermal time value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction XValue = null;
        /// <summary>tm, the time at which the maximum growth RATE is obtained  </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction tm = null;
        /// <summary>te, the time at which Ymax is obtained </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction te = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Sigmoid function</exception>
        public double Value(int arrayIndex = -1)
        {
            try
            {
                //Ymax* (1 + (te - t) / (te - tm)) * (t / te) ^ (te / (te - tm))
                return Ymax.Value(arrayIndex) * (1 + 
                    (te.Value(arrayIndex) - XValue.Value(arrayIndex)) / 
                    (te.Value(arrayIndex) - tm.Value(arrayIndex))) *
                    Math.Pow(XValue.Value(arrayIndex) / te.Value(arrayIndex),
                    te.Value(arrayIndex) / (te.Value(arrayIndex) - tm.Value(arrayIndex)));
                        
            }
            catch (Exception)
            {
                throw new Exception("Error with values to beta growth function");
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

                tags.Add(new AutoDocumentation.Paragraph(" a beta growth function of the form " +
                                                         "y = Ymax * (1 + (te - t)/(te-tm))* (t/te)^(te/(te-tm))", indent));

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, 0, indent+1);
            }
        }
    }
}
