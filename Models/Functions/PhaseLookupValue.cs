using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class PhaseLookupValue : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

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
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

            if (Start == "")
                throw new Exception("Phase start name not set:" + Name);
            if (End == "")
                throw new Exception("Phase end name not set:" + Name);

            if (Phenology != null && Phenology.Between(Start, End) && ChildFunctions.Count > 0)
            {
                IFunction Lookup = ChildFunctions[0] as IFunction;
                return Lookup.Value(arrayIndex);
            }
            else
                return 0.0;
        }

        /// <summary>Gets a value indicating whether [in phase].</summary>
        /// <value><c>true</c> if [in phase]; otherwise, <c>false</c>.</value>
        public bool InPhase
        {
            get
            {
                return Phenology.Between(Start, End);
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

                if (Parent is PhaseLookup)
                {
                    tags.Add(new AutoDocumentation.Paragraph("The value of " + Parent.Name + " from " + Start + " to " + End + " is calculated as follows:", indent));
                    // write children.
                    foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph(this.Value() + " between " + Start + " and " + End + " and a value of zero outside of this period", indent));
                }
            }
        }

    }

}