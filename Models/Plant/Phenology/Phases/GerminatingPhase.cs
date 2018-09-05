using System;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using Models.Soils;
using Models.Functions;


namespace Models.PMF.Phen
{
    /// <summary> This model assumes that germination will be completed on any day after sowing if the extractable soil water is greater than zero.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GerminatingPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Soils.Soil soil = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;


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

        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete { get { return 0.999; } }

        //6. Public methode
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary> Do our timestep development </summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (!phenology.OnStartDayOf("Sowing") && soil.Water[SowLayer] > soil.LL15mm[SowLayer])
            {
                proceedToNextPhase = true;
                propOfDayToUse = 1;
            }

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() { }

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
            SowLayer = Soil.LayerIndexOfDepth(plant.SowingData.Depth, soil.Thickness);
        }
    }
}