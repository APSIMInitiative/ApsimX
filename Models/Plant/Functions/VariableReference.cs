// -----------------------------------------------------------------------
// <copyright file="MathematicalBaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// # [Name]
    /// Return the value of a nominated internal \ref Models.PMF.Plant "Plant" numerical variable
    /// </summary>
    /// \warning You have to specify the full path of numerical variable, which starts from the child of \ref Models.PMF.Plant "Plant".
    /// For example,  <b>[Phenology].ThermalTime.Value</b> refers to value of ThermalTime under phenology function.
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of a nominated internal Plant numerical variable")]
    public class VariableReference : BaseFunction, ICustomDocumentation
    {
        [Link]
        private ILocator locator = null;

        /// <summary>The variable name</summary>
        [Description("Specify an internal Plant variable")]
        public string VariableName { get; set; }

        /// <summary>Gets a double value</summary>
        public override double[] Values()
        {
            object o = locator.Get(VariableName.Trim());
            if (o is IFunction)
                return (o as IFunction).Values();
            else if (o is double[])
                return (double[])o;
            else
                return new double[1] { Convert.ToDouble(o, CultureInfo.InvariantCulture) };
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

                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + StringUtilities.RemoveTrailingString(VariableName, ".Value()") + "</i>", indent));
            }
        }

    }
}