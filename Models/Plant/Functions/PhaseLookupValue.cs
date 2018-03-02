// ----------------------------------------------------------------------
// <copyright file="PhaseLookupValue.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF.Phen;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class PhaseLookupValue : BaseFunction, ICustomDocumentation
    {
        /// <summary>The value being returned</summary>
        private double[] zero = new double[1] { 0 };

        /// <summary>The phenology</summary>
        [Link]
        private Phenology phenologyModel = null;

        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Phase start name not set: + Name
        /// or
        /// Phase end name not set: + Name
        /// </exception>
        public override double[] Values()
        {
            if (Start == "")
                throw new Exception("Phase start name not set:" + Name);
            if (End == "")
                throw new Exception("Phase end name not set:" + Name);

            if (phenologyModel != null && phenologyModel.Between(Start, End) && childFunctions.Count > 0)
            {
                IFunction Lookup = childFunctions[0] as IFunction;
                double[] returnValues = Lookup.Values();
                Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + StringUtilities.BuildString(returnValues, "F3"));
                return returnValues;
            }
            else
            {
                Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:0");
                return zero;
            }
        }

        /// <summary>Gets a value indicating whether [in phase].</summary>
        /// <value><c>true</c> if [in phase]; otherwise, <c>false</c>.</value>
        public bool InPhase
        {
            get
            {
                return phenologyModel.Between(Start, End);
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
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                if (Parent.GetType() == typeof(PhaseLookup))
                {
                    tags.Add(new AutoDocumentation.Paragraph("The value of " + Parent.Name + " from " + Start + " to " + End + " is calculated as follows:", indent));
                    // write children.
                    foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                        AutoDocumentation.DocumentModel(child, tags, -1, indent + 1);
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph(this.Value() + " between " + Start + " and " + End + " and a value of zero outside of this period", indent));
                }
            }
        }

    }

}