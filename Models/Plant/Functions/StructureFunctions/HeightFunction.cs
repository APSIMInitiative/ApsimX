using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Functions.StructureFunctions
{
    [Description("Calculates the potential height increment and then multiplies it by the smallest of any childern functions (Child functions represent stress)")]
    public class HeightFunction : Function
    {
        public Function PotentialHeight { get; set; }
        double PotentialHeightYesterday = 0;
        double Height = 0;
        
        public double DeltaHeight = 0;
        public override void UpdateVariables(string initial)
        {
            double PotentialHeightIncrement = PotentialHeight.FunctionValue - PotentialHeightYesterday;
            double StressValue = 1.0;
            //This function is counting potential height as a stress.
            foreach (Function F in this.Models)
            {
                StressValue = Math.Min(StressValue, F.FunctionValue);
            }
            DeltaHeight = PotentialHeightIncrement * StressValue;
            PotentialHeightYesterday = PotentialHeight.FunctionValue;
            Height += DeltaHeight;
        }
        
        public override double FunctionValue
        {
            get
            {
                return Height;
            }
        }
    }
}
