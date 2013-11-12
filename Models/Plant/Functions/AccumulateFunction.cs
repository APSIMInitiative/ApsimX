using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.Plant.Phen;

namespace Models.Plant.Functions
{
    [Description("Adds the value of all childern functions to the previous days accumulation between start and end phases")]
    public class AccumulateFunction : Function
    {
        //Class members
        public double AccumulatedValue = 0;

        [Link]
        Phenology Phenology = null;

        public string StartStageName = "";

        public string EndStageName = "";

        public double FractionRemovedOnCut = 0; //FIXME: This should be passed from teh manager when "cut event" is called. Must be made general to other events.

        [EventSubscribe("NewMet")]
        private void OnNewMet(Models.WeatherFile.NewMetType NewMet)
        {
            if (Phenology.Between(StartStageName, EndStageName))
            {
                double DailyIncrement = 0.0;
                foreach (Function F in this.Models)
                {
                    DailyIncrement = DailyIncrement + F.Value;
                }
                AccumulatedValue += DailyIncrement;
            }

        }

        
        public override double Value
        {
            get
            {
                return AccumulatedValue;
            }
        }

        [EventSubscribe("Cut")]
        private void OnCut()
        {
            AccumulatedValue -= FractionRemovedOnCut * AccumulatedValue;
        }

    }
}
