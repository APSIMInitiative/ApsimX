using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;


namespace Models.PMF.Phen
{
    /// <summary>
    /// This model assumes that germination will be complete if the extractable soil water is greater than zero on any day after sowing.
    /// </summary>
    /// \pre A \ref Models.Soils.Soil "Soil" function has to exist to 
    /// provide the \ref Models.Soils.SoilWater.esw "extractable soil water (ESW)" 
    /// in the soil profile.
    /// <remarks>
    /// Crop will germinate in the next day if the \ref Models.Soils.SoilWater.esw "extractable soil water (ESW)"
    /// is more than zero.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class GerminatingPhase : Phase
    {
        [Link(IsOptional = true)]
        Soils.Soil Soil = null;

        /// <summary>
        /// Do our timestep development
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            bool CanGerminate = true;
            if (Soil != null)
            {
                CanGerminate = !Phenology.OnDayOf("Sowing") && Soil.SoilWater.ESW > 0;
            }

            if (CanGerminate)
                return 0.999;
            else
                return 0;
        }

        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
        public override double FractionComplete
        {
            get
            {
                return 0.999;
            }
        }

    }
}