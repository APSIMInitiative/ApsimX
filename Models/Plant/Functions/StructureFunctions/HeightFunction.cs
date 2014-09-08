using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions.StructureFunctions
{
    [Serializable]
    [Description("Calculates the potential height increment and then multiplies it by the smallest of any childern functions (Child functions represent stress)")]
    public class HeightFunction : Function
    {
        [Link]
        Function PotentialHeight = null;
        double PotentialHeightYesterday = 0;
        double Height = 0;
        private Model[] ChildFunctions;

        [XmlIgnore]
        public double DeltaHeight { get; set; }
        public override void UpdateVariables(string initial)
        {
            if (ChildFunctions == null)
                ChildFunctions = Children.MatchingMultiple(typeof(Function));

            double PotentialHeightIncrement = PotentialHeight.Value - PotentialHeightYesterday;
            double StressValue = 1.0;
            //This function is counting potential height as a stress.
            foreach (Function F in ChildFunctions)
            {
                StressValue = Math.Min(StressValue, F.Value);
            }
            DeltaHeight = PotentialHeightIncrement * StressValue;
            PotentialHeightYesterday = PotentialHeight.Value;
            Height += DeltaHeight;
        }
        
        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Children.MatchingMultiple(typeof(Function));

                return Height;
            }
        }
    }
}
