using System;
using APSIM.Core;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF
{

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    public class Respiration : Model
    {

        //1. Links
        //------------------------------------------------------------------------------------------------

        // <summary>The parent plant</summary>
        //[Link(Type = LinkType.Ancestor)]
        //private Organ organ = null;

        /// <summary>The amount of carbon lost through maintenance respiration</summary>
        [Link(Type = LinkType.Child)]
        [Units("g/m2/d")]
        private IFunction MaintenanceRespirationFunction = null;

        /// <summary>The carbon cost of moving nutrient out</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientFunctions MobilisationCostFunctions = null;

        /// <summary>The carbon cost of bringing nutrient in</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientFunctions AssimilationCostFunctions = null;

        /// <summary>The carbon cost of moving nutrient out</summary>
        [JsonIgnore]
        public NutrientsStates MobilisationCost { get; private set; }

        /// <summary>The carbon cost of bringing nutrient in</summary>
        [JsonIgnore]
        public NutrientsStates AssimilationCost { get; private set; }

        /// <summary> Maintentnce respiration by the organ</summary>
        [JsonIgnore]
        public double Maintenance { get; private set; }

        /// <summary> growth respiraiton by the organ</summary>
        [JsonIgnore]
        public double Growth { get; private set; }

        /// <summary> respiration for mobilising nutrients</summary>
        [JsonIgnore]
        public double Mobilisation { get; private set; }


        /// <summary>Calculate todays respiration losses</summary>
        public NutrientPoolsState CalculateLosses()
        {
            double storageLoss = 0;
            double metabolisloss = 0;
            return new NutrientPoolsState(0, metabolisloss, storageLoss);
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Maintenance = MaintenanceRespirationFunction.Value();
            MobilisationCost = MobilisationCostFunctions.NutrientValues;
            AssimilationCost = AssimilationCostFunctions.NutrientValues;
        }

        /// <summary> update variables derived from NutrientPoolsStates </summary>
        public void UpdateProperties()
        {

        }

        /// <summary>Constructor </summary>
        public Respiration()
        {
        }

        /// <summary> Clear the components </summary>
        public void Clear()
        {
            UpdateProperties();
        }
    }
}
