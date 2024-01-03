using System;
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




}
