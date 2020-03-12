using System;
using System.Collections.Generic;
using Models.Core;
using System.Xml.Serialization;
using System.IO;
using Models.Soils;
using Models.Functions;


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
        private Soils.Soil soil = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        // 2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The soil layer in which the seed is sown.</summary>
        private int SowLayer = 0;

        // 3. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [XmlIgnore]
        public double FractionComplete { get { return 0.999; } }

        // 4. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
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

        // 5. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when crop is ending.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            SowLayer = soil.LayerIndexOfDepth(plant.SowingData.Depth);
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
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
