using System;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using Models.Functions;


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
    public class GerminatingPhase : Model, IPhase
    {
        [Link(IsOptional = true)]
        Soils.Soil Soil = null;

        [Link]
        private Plant Plant = null;

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>The phenology</summary>
        [Link]
        protected Phenology Phenology = null;

        /// <summary>The thermal time</summary>
        [Link(IsOptional = true)]
        public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        /// <summary>The stress</summary>
        [Link(IsOptional = true)]
        public IFunction Stress = null;

        /// <summary>Adds the specified DLT_TT.</summary>
        public void Add(double dlt_tt) { TTinPhase += dlt_tt; }

        /// <summary>The property of day unused</summary>
        protected double PropOfDayUnused = 0;
        /// <summary>The _ tt for today</summary>
        protected double _TTForToday = 0;

        /// <summary>Gets the tt for today.</summary>
        /// <value>The tt for today.</value>
        public double TTForToday
        {
            get
            {
                if (ThermalTime == null)
                    return 0;
                return ThermalTime.Value();
            }
        }

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>
        /// The soil layer in which the seed is sown
        /// </summary>
        private int SowLayer = 0;

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
        }
        /// <summary>
        /// Do our timestep development
        /// </summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            bool CanGerminate = true;
            if (Soil != null)
            {
                CanGerminate = !Phenology.OnDayOf("Sowing") && Soil.Water[SowLayer] > Soil.LL15mm[SowLayer];
            }
            else if (CanGerminate)
                return 0.999;

            return 0;
        }

        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
       [XmlIgnore]
        public double FractionComplete
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

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            _TTForToday = 0;
            TTinPhase = 0;
            PropOfDayUnused = 0;
        }


        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
        }
    }
}