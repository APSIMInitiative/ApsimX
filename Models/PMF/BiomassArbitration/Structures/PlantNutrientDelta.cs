using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF
{

    /// <summary>
    /// The daily state of flows throughout the plant
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(BiomassArbitrator))]
    public class PlantNutrientsDelta : Model
    {
        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("Supply List of organs to customise order.")]
        public string[] Propertys { get; set; }

        /// <summary>The organs on the plant /// </summary>
        [JsonIgnore]
        public List<OrganNutrientDelta> ArbitratingOrgans { get; set; }

        /// <summary>The total supply of resoure that may be allocated /// </summary>
        public double TotalPlantSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Total); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalReAllocationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.ReAllocation.Total); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalUptakeSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Uptake); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalFixationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.Fixation); } }
        /// <summary>The total supply from Retranslocation  /// </summary>
        public double TotalReTranslocationSupply { get { return ArbitratingOrgans.Sum(o => o.Supplies.ReTranslocation.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemand { get { return ArbitratingOrgans.Sum(o => o.Demands.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantPriorityScalledDemand { get { return ArbitratingOrgans.Sum(o => o.PriorityScaledDemand.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemandsAllocated { get { return ArbitratingOrgans.Sum(o => o.DemandsAllocated.Total); } }

        /// <summary>
        ///  The total amount of reallocation supplies that has been allocated
        /// </summary>
        public double TotalReAllocationSupplyAllocated { get { return ArbitratingOrgans.Sum(o => o.SuppliesAllocated.ReAllocation.Total); } }

        /// <summary>
        ///  The total amount of retranslocation supplies that has been allocated
        /// </summary>
        public double TotalReTranslocationSupplyAllocated { get { return ArbitratingOrgans.Sum(o => o.SuppliesAllocated.ReTranslocation.Total); } }

        /// <summary>
        ///  The total amount of fixation supplies that has been allocated
        /// </summary>
        public double TotalFixationSupplyAllocated { get { return ArbitratingOrgans.Sum(o => o.SuppliesAllocated.Fixation); } }



        //Error checking variables
        /// <summary>Gets or sets the start.</summary>
        [JsonIgnore]
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        [JsonIgnore]
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        [JsonIgnore]
        public double BalanceError { get; set; }

        /// <summary>The constructor</summary>
        public PlantNutrientsDelta()
        {
            ArbitratingOrgans = new List<OrganNutrientDelta>();
            Start = new double();
            End = new double();
            BalanceError = new double();
        }

        /// <summary>Clear</summary>    
        public void Clear()
        {
        }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            ArbitratingOrgans = new List<OrganNutrientDelta>();
            //If Propertys has a list of organ names then use that as a custom ordered list
            var organs = Propertys?.Select(organName => plant.FindChild(organName));
            organs = organs ?? plant.FindAllChildren<Organ>();

            foreach (var organ in organs)
            {
                //Should we throw an exception here if the organ does not have an OrganNutrientDelta? 
                var nutrientDelta = organ.FindChild(Name) as OrganNutrientDelta;
                if (nutrientDelta != null)
                    ArbitratingOrgans.Add(nutrientDelta);
            }

            Start = new double();
            End = new double();
            BalanceError = new double();
        }
    }
}
