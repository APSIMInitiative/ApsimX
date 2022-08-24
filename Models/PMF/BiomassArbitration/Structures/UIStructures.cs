namespace Models.PMF
{
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using System;
    using System.Collections.Generic;

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

    /// <summary>
    /// This class holds the functions for calculating the absolute demands and priorities for each biomass fraction. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientDemandFunctions : Model
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

        /// <summary>Factor for Structural biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStructuralPriority = null;

        /// <summary>Factor for Metabolic biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QMetabolicPriority = null;

        /// <summary>Factor for Storage biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStoragePriority = null;

        /// <summary> The constructor</summary>
        public NutrientDemandFunctions() { }

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

    /// <summary>
    /// This class holds the functions for calculating the Nutrient concentration thresholds
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientConcentrationFunctions : Model, IConcentratinOrFraction
    {
        /// <summary>Maximum Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/ g dWt")]
        public IFunction Maximum = null;
        /// <summary>Critical Nutrient Concentration</summary>
        /// <summary>Maximum Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/ g dWt")]
        public IFunction Critical = null;
        /// <summary>Minimum Nutrient Concentration</summary>
        /// <summary>Maximum Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/ g dWt")]
        public IFunction Minimum = null;

        /// <summary> Interface member that is got by other methods </summary>
        public NutrientPoolsState ConcentrationsOrFractionss
        {
            get
            {
                NutrientPoolsState concentrationOrProportion = new NutrientPoolsState(
                Minimum.Value(),
                Critical.Value(),
                Maximum.Value());
                return concentrationOrProportion;
            }
        }
    }

    /// <summary>
    /// This class holds the functions for calculating the Nutrient concentration thresholds
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientProportionFunctions : Model, IConcentratinOrFraction
    {
        /// <summary>Maximum Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/g Nutrient")]
        public IFunction Structural = null;
        /// <summary>Critical Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/g Nutrient")]
        public IFunction Metabolic = null;
        /// <summary>Minimum Nutrient Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g Nutrient/g Nutrient")]
        public IFunction Storage = null;

        /// <summary> Interface member that is got by other methods </summary>
        public NutrientPoolsState ConcentrationsOrFractionss
        {
            get
            {
                NutrientPoolsState concentrationOrProportion = new NutrientPoolsState(
                    Structural.Value(),
                    Metabolic.Value(),
                    Storage.Value());
                return concentrationOrProportion;
            }
        }
    }

    /// <summary>
    /// This class holds the functions for calculating the Nutrient concentration thresholds
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Respiration))]
    public class NutrientFunctions : Model
    {
        /// <summary>Parameter relevent to Carbon</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction C = null;
        /// <summary>Parameter relevent to Nitrogen</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction N = null;
        /// <summary>Parameter relevent to Phosphorus</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction P = null;
        /// <summary>Parameter relevent to Potassium</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction K = null;

        /// <summary> Interface member that is got by other methods </summary>
        public NutrientsStates NutrientValues
        {
            get
            {
                NutrientsStates states = new NutrientsStates(
                    C.Value(),
                    N.Value(),
                    P.Value(),
                    K.Value());
                return states;
            }
        }
    }

    /// <summary>
    /// Interface class for Uptake Methods.
    /// </summary>
    public interface IConcentratinOrFraction

    {
        /// <summary>
        /// Nutrient concnetration or fraction values
        /// </summary>
        NutrientPoolsState ConcentrationsOrFractionss { get; }
    }


}
