namespace Models.PMF
{
    using System;
    using Models.Soils;
    using Models.Core;
    using PMF.Interfaces;
    using System.Collections.Generic;

    /// <summary>
    /// An event arguments class for some events.
    /// </summary>
    public class ModelArgs : EventArgs
    {
        /// <summary>
        /// The model
        /// </summary>
        public IModel Model;
    }

    /// <summary>
    /// 
    /// </summary>
    public class WaterUptakesCalculatedUptakesType
    {
        /// <summary>The name</summary>
        public String Name = "";
        /// <summary>The amount</summary>
        public Double[] Amount;
    }
    /// <summary>
    /// 
    /// </summary>
    public class WaterUptakesCalculatedType
    {
        /// <summary>The uptakes</summary>
        public WaterUptakesCalculatedUptakesType[] Uptakes;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void WaterUptakesCalculatedDelegate(WaterUptakesCalculatedType Data);
    /// <summary>
    /// 
    /// </summary>
    public class WaterChangedType
    {
        /// <summary>The delta water</summary>
        public Double[] DeltaWater;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void WaterChangedDelegate(WaterChangedType Data);
    /// <summary>
    /// 
    /// </summary>
    public class PruneType
    {
        /// <summary>The bud number</summary>
        public Double BudNumber;
    }
    /// <summary>
    /// 
    /// </summary>
    public class KillLeafType
    {
        /// <summary>The kill fraction</summary>
        public Single KillFraction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void FOMLayerDelegate(FOMLayerType Data);
    /// <summary>
    /// 
    /// </summary>
    public delegate void NullTypeDelegate();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void NewCropDelegate(NewCropType Data);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SowPlant2Type : EventArgs
    {
        /// <summary>The parent plant</summary>
        public Plant Plant = null;

        /// <summary>The cultivar</summary>
        public String Cultivar { get; set; }
        /// <summary>The population</summary>
        public Double Population { get; set; }
        /// <summary>The depth</summary>
        public Double Depth { get; set; }
        /// <summary>The row spacing</summary>
        public Double RowSpacing { get; set; }
        /// <summary>The maximum cover</summary>
        public Double MaxCover { get; set; }
        /// <summary>The bud number</summary>
        public Double BudNumber { get; set; }
        /// <summary>The skip row</summary>
        public Double SkipRow { get; set; }
        /// <summary>The skip plant</summary>
        public Double SkipPlant { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SowPlant2Type"/> class.
        /// </summary>
        public SowPlant2Type()
        {
            Cultivar = "";
            Population = 100;
            Depth = 100;
            RowSpacing = 150;
            MaxCover = 1;
            BudNumber = 1;
        }


    }
    /// <summary>
    /// 
    /// </summary>
    public class BiomassRemovedType
    {
        /// <summary>The crop_type</summary>
        public String crop_type = "";
        /// <summary>The dm_type</summary>
        public String[] dm_type;
        /// <summary>The dlt_crop_dm</summary>
        public Single[] dlt_crop_dm;
        /// <summary>The DLT_DM_N</summary>
        public Single[] dlt_dm_n;
        /// <summary>The DLT_DM_P</summary>
        public Single[] dlt_dm_p;
        /// <summary>The fraction_to_residue</summary>
        public Single[] fraction_to_residue;
    }

    /// <summary>
    /// Event arguments when biomass is removed.
    /// </summary>
    public class RemovingBiomassArgs : EventArgs
    {
        /// <summary>
        /// Type of biomass removal.
        /// </summary>
        public string biomassRemoveType;

        /// <summary>
        /// Removal fractions for each organ.
        /// </summary>
        public Dictionary<string, OrganBiomassRemovalType> removalData = new Dictionary<string, OrganBiomassRemovalType>();
    }

    /// <summary>
    /// 
    /// </summary>
    public class NewCropType
    {
        /// <summary>The sender</summary>
        public String sender = "";
        /// <summary>The crop_type</summary>
        public String crop_type = "";
    }

    ///<summary>Data passed to each organ when a biomass remove event occurs.  The proportion of biomass removed from each organ is the sum of the FractionRemoved and the FractionToRedidues</summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class OrganBiomassRemovalType : Model
    {
        /// <summary>
        /// The amount of biomass taken from each organ and removeed from the zone on harvest, cut, graze or prune.
        /// </summary>
        [Description("Fraction of biomass to remove from plant (lost to system)")]
        public double FractionRemoved { get; set; }
        /// <summary>
        /// The amount of biomass to removed from each organ and passed to residue pool on on harvest, cut, graze or prune
        /// </summary>
        [Description("Fraction of biomass to remove from plant (and send to surface organic matter")]
        public double FractionToResidue { get; set; }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            double totalPercent = (FractionRemoved + FractionToResidue) * 100;

            string text = "If a **" + Name.ToLower() + "** is performed and no fractions are specified then " + totalPercent + "% of " +
                           Parent.Parent.Name.ToLower() + " biomass will be removed";
            if (FractionToResidue == 0)
                text += " with none of it going to the surface organic matter pool";
            else if (FractionRemoved == 0)
                text += " with all of it going to the surface organic matter pool";
            else
                text += " with " + (FractionToResidue * 100) + "% of it going to the surface organic matter pool";
            tags.Add(new AutoDocumentation.Paragraph(text, indent));
        }
    }

    ///<summary>Data structure to hold removal and residue returns fractions for all plant organs</summary>
    [Serializable]
    public class RemovalFractions
    {
        /// <summary>
        /// The list of BiomassRemovalTypes for each organ
        ///</summary>
        private Dictionary<string, OrganBiomassRemovalType> removalValues = new Dictionary<string, OrganBiomassRemovalType>();
        /// <summary>
        /// The Phenological stage that biomass removal resets phenology to.
        ///</summary>
        public double SetThinningProportion { get; set; }

        /// <summary>
        /// The Phenological stage that biomass removal resets phenology to.
        ///</summary>
        public double SetPhenologyStage { get; set; }

        /// <summary>
        /// Method to set the FractionRemoved for specified Organ
        ///</summary>
        public void SetFractionRemoved(string organName, double fraction)
        {
            if (removalValues.ContainsKey(organName))
                removalValues[organName].FractionRemoved = fraction;
            else
                removalValues.Add(organName, new OrganBiomassRemovalType() { FractionRemoved = fraction });
        }
        
        /// <summary>
        /// Method to set the FractionToResidue for specified Organ
        ///</summary>
        public void SetFractionToResidue(string organName, double fraction)
        {
            if (removalValues.ContainsKey(organName))
                removalValues[organName].FractionToResidue = fraction;
            else
                removalValues.Add(organName, new OrganBiomassRemovalType() { FractionToResidue = fraction });
        }

        /// <summary>
        /// Gets the removal fractions for the specified organ or null if not found.
        /// </summary>
        /// <param name="organName">The organ name to look for.</param>
        public OrganBiomassRemovalType GetFractionsForOrgan(string organName)
        {
            if (removalValues.ContainsKey(organName))
                return removalValues[organName];
            else
                return null;
        }
    }
}
