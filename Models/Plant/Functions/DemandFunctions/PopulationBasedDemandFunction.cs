using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Population based demand function
    /// </summary>
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function calculates DM demand from the start stage over the growth duration as the product of potential growth rate (MaximumOrganWt/GrowthDuration) and daily thermal time. It returns the product of this potential rate and any childern so if other stress multipliers are required they can be constructed with generic functions.  Stress factors are optional")]
    public class PopulationBasedDemandFunction : Function
    {
        /// <summary>The thermal time</summary>
        [Link]
        Function ThermalTime = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The phenology</summary>
        [Link(IsOptional = true)]
        private Models.PMF.Phen.Phenology Phenology = null;

        /// <summary>The expansion stress</summary>
        [Link]
        Function ExpansionStress = null;

        /// <summary>The maximum organ wt</summary>
        [Description("Size individual organs will grow to when fully supplied with DM")]
        public double MaximumOrganWt = 0;

        /// <summary>The start stage</summary>
        [Description("Stage when organ growth starts ")]
        public double StartStage = 0;

        /// <summary>The growth duration</summary>
        [Description("ThermalTime duration of organ growth ")]
        public double GrowthDuration = 0;

        /// <summary>The accumulated thermal time</summary>
        private double AccumulatedThermalTime = 0;
        /// <summary>The thermal time today</summary>
        private double ThermalTimeToday = 0;

        /// <summary>Called when [new weather data available].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {
            if ((Phenology.Stage >= StartStage) && (AccumulatedThermalTime < GrowthDuration))
            {
                ThermalTimeToday = Math.Min(ThermalTime.Value, GrowthDuration - AccumulatedThermalTime);
                AccumulatedThermalTime += ThermalTimeToday;
            }
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                double Value = 0.0;
                if ((Phenology.Stage >= StartStage) && (AccumulatedThermalTime < GrowthDuration))
                {
                    double Rate = MaximumOrganWt / GrowthDuration;
                    Value = Rate * ThermalTimeToday * Structure.TotalStemPopn;
                }

                return Value * ExpansionStress.Value;
            }
        }

    }
}


