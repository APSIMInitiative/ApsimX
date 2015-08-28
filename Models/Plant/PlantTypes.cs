namespace Models.PMF
{
    using System;
    using Models.Soils;
    using Models.Core;
    using PMF.Interfaces;

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
    /// <param name="NewCanopyData">The new canopy data.</param>
    public delegate void NewCanopyDelegate(NewCanopyType NewCanopyData);
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
    [Serializable]
    public class OrganBiomassRemovalType
    {
        /// <summary>
        /// The name of each organ
        /// </summary>
        public string NameOfOrgan { get; set; }
        /// <summary>
        /// The amount of biomass taken from each organ and removeed from the zone on harvest, cut, graze or prune.
        /// </summary>
        public double FractionRemoved { get; set; }
        /// <summary>
        /// The amount of biomass to removed from each organ and passed to residue pool on on harvest, cut, graze or prune
        /// </summary>
        public double FractionToResidue { get; set; }
        /// <summary>
        /// Removal method specifies for cohorted organs (leaf) if biomass removal events take biomass from the top, bottom or side of cohorts.
        /// </summary>
        public string RemovalMethod { get; set; }
    }

    ///<summary>Data structure to hold removal and residue returns fractions for all plant organs</summary>
    [Serializable]
    public class RemovalFractions
    {
        /// <summary>
        /// The list of BiomassRemovalTypes for each organ
        ///</summary>
        public OrganBiomassRemovalType[] OrganList {get; set;}
        
        ///<summary>
        ///Method to construct list
        ///</summary>
        public RemovalFractions(IOrgan[] Organs)
        {
            OrganList = new OrganBiomassRemovalType[Organs.Length];
            int k = 0;
            foreach (IOrgan o in Organs)
            {
                OrganList[k] = new OrganBiomassRemovalType();
                OrganList[k].NameOfOrgan = o.Name;
                k += 1;
            }
        }

        ///<summary>
        ///Default constructor
        ///</summary>
        public RemovalFractions() { }

        /// <summary>
        /// Method to set the FractionRemoved for specified Organ
        ///</summary>
        public void SetFractionRemoved(string organName, double Fraction)
        {
            bool Matchfound = false;
            foreach (OrganBiomassRemovalType r in OrganList)
                if (String.Equals(r.NameOfOrgan, organName, StringComparison.OrdinalIgnoreCase))
                {
                    r.FractionRemoved = Fraction;
                    Matchfound = true;
                }
            if (Matchfound == false)
            throw new Exception("The plant model could not find an organ with a name that matches " + organName+ " when matching FractionRemoved. Check your spelling?");
        }
        
        /// <summary>
        /// Method to set the FractionToResidue for specified Organ
        ///</summary>
        public void SetFractionToResidue(string organName, double Fraction)
        {
            bool Matchfound = false;
            foreach (OrganBiomassRemovalType r in OrganList)
                if (String.Equals(r.NameOfOrgan, organName, StringComparison.OrdinalIgnoreCase))
                {
                    r.FractionRemoved = Fraction;
                    Matchfound = true;
                }
            if (Matchfound == false)
            throw new Exception("The plant model could not find an organ with a name that matches " + organName + " when matching FractionToResidue. Check your spelling?");
        }
    }
}
