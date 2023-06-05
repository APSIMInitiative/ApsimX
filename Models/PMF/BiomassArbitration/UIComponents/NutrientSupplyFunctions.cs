using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating the Nutrient supplies from the organ. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientSupplyFunctions : Model
    {
        /// <summary>The supply from reallocaiton from senesed material</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public NutrientPoolFunctions ReAllocation = null;

        /// <summary>The supply from uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Uptake = null;

        /// <summary>The supply from fixation.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Fixation = null;

        /// <summary>The supply from retranslocation of storage</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public NutrientPoolFunctions ReTranslocation = null;

        /// <summary> The constructor</summary>
        public NutrientSupplyFunctions() { }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {

            // add a heading
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // get description of this class.
            tags.Add(new AutoDocumentation.Paragraph("This is the collection of functions for calculating the demands for each of the biomass pools (Structural, Metabolic, and Storage).", indent));

            // write memos.
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

            // write children.
            foreach (IModel child in this.FindAllChildren<IFunction>())
                AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);

        }
    }



}
