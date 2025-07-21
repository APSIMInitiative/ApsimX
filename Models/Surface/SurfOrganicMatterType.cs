using System;
using System.Text.Json.Serialization;
using Models.Core;
using Models.Interfaces;

namespace Models.Surface
{

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public class SurfOrganicMatterType
    {
        /// <summary>The name</summary>
        public string name;
        /// <summary>The organic matter type</summary>
        public string OrganicMatterType;
        /// <summary>The pot decomp rate</summary>
        public double PotDecompRate;
        /// <summary>The no3</summary>
        public double no3;
        /// <summary>The NH4</summary>
        public double nh4;
        /// <summary>The po4</summary>
        public double po4;
        /// <summary>The standing</summary>
        public OMFractionType[] Standing;
        /// <summary>The lying</summary>
        public OMFractionType[] Lying;
        /// <summary>The canopy of the standing</summary>
        public ResidueCanopy CanopyStanding = new ResidueCanopy();
        /// <summary>The canopy of the lying</summary>
        public ResidueCanopy CanopyLying = new ResidueCanopy();

        /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
        public SurfOrganicMatterType()
        {
            name = null;
            OrganicMatterType = null;
            PotDecompRate = 0;
            no3 = 0;
            nh4 = 0;
            po4 = 0;
            Standing = new OMFractionType[SurfaceOrganicMatter.maxFr];
            Lying = new OMFractionType[SurfaceOrganicMatter.maxFr];


            for (int i = 0; i < SurfaceOrganicMatter.maxFr; i++)
            {
                Lying[i] = new OMFractionType();
                Standing[i] = new OMFractionType();
            }
        }

        /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public SurfOrganicMatterType(string name, string type)
            : this()
        {
            this.name = name;
            OrganicMatterType = type;
        }

        /// <summary>
        /// Remove a fraction of the material.
        /// </summary>
        /// <param name="fIncorp">The fraction to remove.</param>
        /// <returns>The amount removed.</returns>
        internal OMFractionType Remove(double fIncorp)
        {
            OMFractionType removed = new();
            for (int pool = 0; pool < SurfaceOrganicMatter.maxFr; pool++)
            {
                var removedFromPool = Lying[pool].Remove(fIncorp);
                removed.Add(removedFromPool);

                removedFromPool = Standing[pool].Remove(fIncorp);
                removed.Add(removedFromPool);
            }

            return removed;
        }

    }

    /// <summary>
    /// Class that holds Icanopy interface members
    /// </summary>
    public class ResidueCanopy : ICanopy
    {
        /// <summary>Canopy type identifier.</summary>
        public string CanopyType { get; set; } = "Residue";

        /// <summary>Albedo.</summary>
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        public double Gsmax { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get; set; }

        /// <summary>Gets the LAI (m^2/m^2)</summary>
        public double LAI { get; set; }

        /// <summary>Gets the maximum LAI (m^2/m^2)</summary>
        public double LAITotal { get; set; }

        /// <summary>Gets the cover green (0-1)</summary>
        public double CoverGreen { get; set; }

        /// <summary>Gets the cover total (0-1)</summary>
        public double CoverTotal { get; set; }

        /// <summary>Gets the canopy height (mm)</summary>
        public double Height { get; set; }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Depth { get; set; }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Width { get; set; }

        /// <summary>Sets the potential evapotranspiration.</summary>
        public double PotentialEP { get; set; }

        /// <summary>The pe tr</summary>
        [Units("mm")]
        [JsonIgnore]
        public double PETr { get; set; }

        /// <summary>The pe ta</summary>
        [JsonIgnore]
        [Units("mm")]
        public double PETa { get; set; }


        /// <summary>Sets the actual water demand.</summary>
        public double WaterDemand { get; set; }

        /// <summary>Sets the light profile.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>Initializes a new instance of the <see cref="OMFractionType"/> class.</summary>
        public ResidueCanopy()
        {
            Albedo = 0.5;
            Gsmax = 0;
            R50 = 0;
            LAI = 0;
            //LAITotal = 0;
            CoverGreen = 0;
            //CoverTotal = 0;
            //Height = 0;
            //Depth = 0;
            Width = 0;
            PotentialEP = 0;
            WaterDemand = 0;
        }
    }
}
