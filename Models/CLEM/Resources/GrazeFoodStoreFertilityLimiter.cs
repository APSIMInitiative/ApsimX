using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Reduces new pasture growth Nitrogen content (N%) based on rules
    /// Allows for soil fertility to be implied from pasture production data
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStoreType))]
    [Description("Allows for the reduction of new pasture nitrogen content (N%) based on annual yield or growth month")]
    [Version(1, 0, 1, "Provides NABSA 'Fertility - N decline yield' functionality")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreFertilityLimiter.htm")]
    public class GrazeFoodStoreFertilityLimiter: CLEMModel
    {
        [Link]
        Clock Clock = null;

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
        [System.ComponentModel.DefaultValue(typeof(MonthsOfYear), "January")]
        [Required, Month]
        public MonthsOfYear AnnualYieldStartMonth { get; set; }

        /// <summary>
        /// Proportional reduction in N%
        /// </summary>
        [Description("Nitrogen reduction to apply")]
        [System.ComponentModel.DefaultValue(typeof(Single), "0.2")]
        [Required]
        [Proportion]
        [GreaterThanValue(0)]
        public Single NitrogenReduction { get; set; }

        private double annualNUsed = 0;
        private GrazeFoodStoreType parentPasture;
        private bool timingPresent;

        /// <summary>
        /// Constructor
        /// </summary>
        public GrazeFoodStoreFertilityLimiter()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentPasture = this.Parent as GrazeFoodStoreType;
            timingPresent = FindAllChildren<ActivityTimerMonthRange>().Count() >= 1;
        }

        /// <summary>
        /// Check for N reduction due to pasture yield or month based on latest growth
        /// </summary>
        /// <param name="newGrowthKgHa">The amount of new growth in this month (kg per ha)</param>
        /// <returns>The proportion of new grass nitrogen content to assign</returns>
        public double GetProportionNitrogenLimited(double newGrowthKgHa) 
        {
            // calculate proportion of new growth above the cutoff to adjust N reduction accordingly
            double nRequired = newGrowthKgHa * parentPasture.GreenNitrogen / 100;
            double reduction;

            if (timingPresent && this.TimingOK)
            {
                // if timer present and inside month range, return reduced N proportion
                reduction = 1 - NitrogenReduction;
            }
            else
            {
                // calculate proportion N based on N required and already used for year
                double shortfall = Math.Min(nRequired, Math.Max(0, nRequired - (AnnualNitrogenSupply- annualNUsed)));
                reduction = ((shortfall * (1 - NitrogenReduction)) + (nRequired - shortfall)) / nRequired;
            }
            annualNUsed += (nRequired*reduction);
            return reduction;
        }

        /// <summary>An event handler to allow us to reset annual yield in the specified month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            if(Clock.Today.Month == (int)AnnualYieldStartMonth)
            {
                annualNUsed = 0;
            }
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            bool timerpresent = FindAllChildren<ActivityTimerMonthRange>().Count() > 0;
            parentPasture = this.Parent as GrazeFoodStoreType;

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\nThe nitrogen content of new pasture will be reduced by ");
                if (NitrogenReduction == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not Set</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(NitrogenReduction.ToString("P0"));
                    htmlWriter.Write("</span>");
                }
                htmlWriter.Write(" if");

                if (timerpresent)
                {
                    htmlWriter.Write("</div>");
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("<b>(A)</b>");
                }

                htmlWriter.Write(" an annual nitrogen supply of  ");
                if (AnnualNitrogenSupply == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not Set</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(AnnualNitrogenSupply.ToString("N0"));
                    htmlWriter.Write("</span> kg per hectare has been used since ");
                }
                if (AnnualYieldStartMonth == MonthsOfYear.NotSet)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Month not set");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(AnnualYieldStartMonth.ToString());
                }
                htmlWriter.Write("</span>\r\n</div>");

                if (AnnualNitrogenSupply > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    if (parentPasture.GreenNitrogen > 0)
                    {
                        htmlWriter.Write($"This equates to <span class=\"setvalue\">{AnnualNitrogenSupply / (parentPasture.GreenNitrogen / 100)}</span> kg per hectare of pasture production given the new growth nitrogen content of <span class=\"setvalue\">{parentPasture.GreenNitrogen}%</span>.");
                    }
                    else
                    {
                        htmlWriter.Write($"This equates to <span class=\"errorlink\">Undefined</span> kg per hectare of pasture production given the green growth nitrogen content of <span class=\"errorlink\">Not set</span>.");
                    }
                    htmlWriter.Write("\r\n</div>");
                }

                if (timerpresent)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("or <b>(B)</b> the growth month falls within the specified period below");
                    htmlWriter.Write("\r\n</div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("or <b>(B)</b> Add a ActivityMonthRangeTimer below to reduce nitrogen content in specified months");
                    htmlWriter.Write("\r\n</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
