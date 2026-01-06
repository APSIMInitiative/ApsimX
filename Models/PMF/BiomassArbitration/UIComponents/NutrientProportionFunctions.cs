using System;
using APSIM.Core;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{

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

}
