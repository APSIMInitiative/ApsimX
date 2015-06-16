using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Phen
{

    /// <summary>The end phase in phenology</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class EndPhase : Phase
    {
        /// <summary>The _ cumulative value</summary>
        private double _CumulativeValue;


        /// <summary>Gets the cumulative value.</summary>
        /// <value>The cumulative value.</value>
        public double CumulativeValue { get { return _CumulativeValue; } }

        /// <summary>Do our timestep development</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            _CumulativeValue += ThermalTime.Value;
            return 0;
        }

        /// <summary>Return a fraction of phase complete.</summary>
        /// <value>The fraction complete.</value>
        public override double FractionComplete { get { return 0.0; } }

        /// <summary>Resets the phase.</summary>
        public override void ResetPhase()
        {
            base.ResetPhase();
            _CumulativeValue = 0;
        }
    }
}

      
      
