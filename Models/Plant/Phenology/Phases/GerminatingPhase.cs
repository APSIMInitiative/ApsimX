using System;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using Models.Functions;


namespace Models.PMF.Phen
{
    /// <summary> This model assumes that germination will be completed on any day after sowing if the extractable soil water is greater than zero. /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class GerminatingPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Soils.Soil Soil = null;

        [Link]
        private Plant Plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.
        

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The soil layer in which the seed is sown</summary>
        private int SowLayer = 0;

        
        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Gets the tt for today.</summary>
        [XmlIgnore]
        public double TTForToday { get { return ThermalTime.Value(); } }

        /// <summary>Gets the t tin phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                return 0.999;
            }
            set
            {
                if (phenology != null)
                    throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }

        //6. Public methode
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary> Do our timestep development </summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            bool CanGerminate = true;
            if (Soil != null)
            {
                CanGerminate = !phenology.OnStartDayOf("Sowing") && Soil.Water[SowLayer] > Soil.LL15mm[SowLayer];
            }

            if (CanGerminate)
                return 0.999;
            else
                return 0;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            TTinPhase = 0;
        }

        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
        }


        //7. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when crop is ending</summary>
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

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
    }
}