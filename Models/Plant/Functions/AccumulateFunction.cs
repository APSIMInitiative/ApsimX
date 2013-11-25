using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    [Description("Adds the value of all childern functions to the previous days accumulation between start and end phases")]
    public class AccumulateFunction : Function
    {
        //Class members
        private double AccumulatedValue = 0;

        [Link]
        Phenology Phenology = null;

        public List<Function> Children { get; set; }
 

        public string StartStageName = "";

        public string EndStageName = "";

        private double FractionRemovedOnCut = 0; //FIXME: This should be passed from teh manager when "cut event" is called. Must be made general to other events.

        [EventSubscribe("NewMet")]
        private void OnNewMet(Models.WeatherFile.NewMetType NewMet)
        {
            if (Phenology.Between(StartStageName, EndStageName))
            {
                double DailyIncrement = 0.0;
                foreach (Function F in Children)
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
