using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating values for each Nutrient component. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    [ValidParent(ParentType = typeof(NutrientSupplyFunctions))]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class NutrientPoolFunctions : Model
    {
        /// <summary>The demand for the structural fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Structural = null;

        /// <summary>The demand for the metabolic fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Metabolic = null;

        /// <summary>The demand for the storage fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Storage = null;

        /// <summary> The constructor</summary>
        public NutrientPoolFunctions() { }
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
