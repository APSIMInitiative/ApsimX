using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function calculates DM demand from the start stage over the growth duration as the product of potential growth rate (MaximumOrganWt/GrowthDuration) and daily thermal time. It returns the product of this potential rate and any childern so if other stress multipliers are required they can be constructed with generic functions.  Stress factors are optional")]
    public class PopulationBasedDemandFunction : Function
    {
        [Link]
        Function ThermalTime = null;

        [Link]
        Structure Structure = null;

        [Link]
        StageBasedInterpolation StageCode = null;

        [Link]
        Function ExpansionStress = null;

        [Description("Size individual organs will grow to when fully supplied with DM")]
        public double MaximumOrganWt = 0;

        [Description("Stage when organ growth starts ")]
        public double StartStage = 0;

        [Description("ThermalTime duration of organ growth ")]
        public double GrowthDuration = 0;

        private double AccumulatedThermalTime = 0;
        private double ThermalTimeToday = 0;

        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(Models.WeatherFile.NewMetType NewMet)
        {
            if ((StageCode.Value >= StartStage) && (AccumulatedThermalTime < GrowthDuration))
            {
                ThermalTimeToday = Math.Min(ThermalTime.Value, GrowthDuration - AccumulatedThermalTime);
                AccumulatedThermalTime += ThermalTimeToday;
            }
        }

        
        public override double Value
        {
            get
            {
                double Value = 0.0;
                if ((StageCode.Value >= StartStage) && (AccumulatedThermalTime < GrowthDuration))
                {
                    double Rate = MaximumOrganWt / GrowthDuration;
                    Value = Rate * ThermalTimeToday * Structure.TotalStemPopn;
                }

                return Value * ExpansionStress.Value;
            }
        }

    }
}


