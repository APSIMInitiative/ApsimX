using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Phen
{
    public class EndPhase : Phase
    {
        private double _CumulativeValue;

        
        public double CumulativeValue { get { return _CumulativeValue; } }

        /// <summary>
        /// Do our timestep development
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            _CumulativeValue += ThermalTime.FunctionValue;
            return 0;
        }

        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
        public override double FractionComplete { get { return 0.0; } }

    }
}

      
      
