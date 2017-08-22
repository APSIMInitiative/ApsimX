using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.OldPlant;


namespace Models.PMF.Phen
{
    /// <summary>
    /// This model assumes that germination will be completed on any day after sowing if the extractable soil water is greater than zero.
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

        [Link]
        ISummary summary = null;

        [Link]
        Phenology phenology = null;

        [Link(IsOptional = true)]
        private Plant Plant = null;

        [Link(IsOptional = true)]
        private Plant15 Plant15 = null;    // This can be deleted once we get rid of plant15.

        /// <summary>
        /// The soil layer in which the seed is sown
        /// </summary>
        private int SowLayer = 0;

        /// <summary>Number of days from sowing to end of this phase.</summary>
        [XmlIgnore]
        public int DaysFromSowingToEndPhase { get; set; }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            double SowDepth = 0;
            double accumDepth = 0;
            if (Plant != null)
                SowDepth = Plant.SowingData.Depth;  
            if (Plant15 != null)
                SowDepth = Plant15.SowingData.Depth;
            bool layerfound = false;
            for (int layer = 0; layerfound; layer++)
            {
                accumDepth += Soil.Thickness[layer];
                if (SowDepth <= accumDepth)
                {
                    SowLayer = layer;
                    layerfound = true;
                }
            }
            if (DaysFromSowingToEndPhase > 0)
                summary.WriteMessage(this, "FIXED number of days from sowing to " + Name + " = " + DaysFromSowingToEndPhase);
        }
        /// <summary>
        /// Do our timestep development
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            bool CanGerminate = true;
            if (Soil != null)
            {
                CanGerminate = !Phenology.OnDayOf("Sowing") && Soil.Water[SowLayer] > Soil.SoilWater.LL15mm[SowLayer];
            }

            if (DaysFromSowingToEndPhase > 0)
            {
                if (phenology.DaysAfterSowing >= DaysFromSowingToEndPhase)
                    return 0.999;
            }
            else if (CanGerminate)
                return 0.999;

            return 0;
        }

        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
       [XmlIgnore]
        public override double FractionComplete
        {
            get
            {
                return 0.999;
            }
            set
            {
                if (Phenology != null)
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }
    }
}