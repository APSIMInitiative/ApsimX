using Models.CLEM.Timers;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Reduces new pasture growth Nitrogen content (N%) based on rules
    /// Allows for soil fertility to be implied from pasture production data
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStoreType))]
    [Description("Allows for the reduction of new pasture nitrogen content (N%) based on annual yield or growth month")]
    [Version(1, 0, 1, "Provides NABSA 'Fertility - N decline yield' functionality")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreFertilityLimiter.htm")]
    public class GrazeFoodStoreFertilityLimiter : CLEMModel
    {
        [Link]
        private readonly IClock clock = null;

        private double annualNUsed = 0;
        private GrazeFoodStoreType parentPasture;
        private bool timingPresent;

        /// <summary>
        /// Annual supply of N (kg per ha) before there is a nitrogen reduction in new growth
        /// </summary>
        [Description("Annual nitrogen supply (kg/ha)")]
        [Required]
        [GreaterThanValue(0)]
        public double AnnualNitrogenSupply { get; set; }

        /// <summary>
        /// Month in which to start calculating annual pasture yield
        /// </summary>
        [Description("First month of annual pasture yield")]
        [Required, Month]
        public MonthsOfYear AnnualYieldStartMonth { get; set; } = MonthsOfYear.January;

        /// <summary>
        /// Proportional reduction in N%
        /// </summary>
        [Description("Nitrogen reduction to apply")]
        [Required]
        [Proportion]
        [GreaterThanValue(0)]
        public Single NitrogenReduction { get; set; } = 0.2f;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentPasture = this.Parent as GrazeFoodStoreType;
            timingPresent = Structure.FindChildren<ActivityTimerMonthRange>().Any();
        }

        /// <summary>
        /// Check for N reduction due to pasture yield or month based on latest growth
        /// </summary>
        /// <param name="newGrowthKgHa">The amount of new growth in this month (kg per ha)</param>
        /// <returns>The proportion of new grass nitrogen content to assign</returns>
        public double GetProportionNitrogenLimited(double newGrowthKgHa)
        {
            // calculate proportion of new growth above the cutoff to adjust N reduction accordingly
            double nRequired = newGrowthKgHa * parentPasture.GreenNitrogenPercent / 100;
            double reduction;

            if (timingPresent && this.TimingOK)
            {
                // if timer present and inside month range, return reduced N proportion
                reduction = 1 - NitrogenReduction;
            }
            else
            {
                // calculate proportion N based on N required and already used for year
                double shortfall = Math.Min(nRequired, Math.Max(0, nRequired - (AnnualNitrogenSupply - annualNUsed)));
                reduction = ((shortfall * (1 - NitrogenReduction)) + (nRequired - shortfall)) / nRequired;
            }
            annualNUsed += (nRequired * reduction);
            return reduction;
        }

        /// <summary>An event handler to allow us to reset annual yield in the specified month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            if (clock.Today.Month == (int)AnnualYieldStartMonth)
            {
                annualNUsed = 0;
            }
        }
    }
}
