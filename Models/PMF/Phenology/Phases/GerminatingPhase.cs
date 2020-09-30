using System;
using System.Collections.Generic;
using Models.Core;
using Newtonsoft.Json;
using System.IO;
using Models.Soils;
using Models.Functions;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Phen
{
    /// <summary>Describe the phenological development through the germination.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GerminatingPhase : Model, IPhase, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Link to the soil water balance.</summary>
        [Link]
        private ISoilWater waterBalance = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private Clock clock = null;

        // 2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary>The soil layer in which the seed is sown.</summary>
        private int SowLayer = 0;


        // 3. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SeedImbibed;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete { get { return 0.999; } }

        /// <summary>
        /// Date for germination to occur.  null by default so model is used
        /// </summary>
        [JsonIgnore]
        public string GerminationDate { get; set; }

        // 4. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (GerminationDate != null)
            {
                if (DateUtilities.DatesEqual(GerminationDate, clock.Today))
                {
                    doGermination(ref proceedToNextPhase, ref propOfDayToUse);
                }
            }

            else if (!phenology.OnStartDayOf("Sowing") && waterBalance.SWmm[SowLayer] > soilPhysical.LL15mm[SowLayer])
            {
                doGermination(ref proceedToNextPhase, ref propOfDayToUse);
            }

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() { GerminationDate = null; }

        // 5. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        private void doGermination(ref bool proceedToNextPhase, ref double propOfDayToUse)
        {
            if (SeedImbibed != null)
                SeedImbibed.Invoke(this, new EventArgs());
            proceedToNextPhase = true;
            propOfDayToUse = 1;
        }

        /// <summary>Called when crop is ending.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            SowLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, plant.SowingData.Depth);
        }

        /// <summary>Writes documentation for this class by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // write description of this class
                tags.Add(new AutoDocumentation.Paragraph("The model assumes that germination will be completed on the day after sowing, "
                    + "provided that the extractable soil water is greater than zero.", indent));

                // write memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
