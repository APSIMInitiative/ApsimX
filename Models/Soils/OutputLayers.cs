namespace Models.Soils
{
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using Models.Soils.Standardiser;
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class takes soil variables simulated at each of the modelled soil layers and maps them onto a new specified layering.
    /// The outputs can be used for producing summaries and to facilitate comparison with observed data.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class OutputLayers : Model
    {
        /// <summary>Access the soil physical properties.</summary>
        [Link] 
        private IPhysical soilPhysical = null;
        
        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private ISoilWater waterBalance = null;
        
        [Link]
        Soils.Sample initial = null;

        private ISolute NO3Solute = null;
        private ISolute NH4Solute = null;
        private ISolute UreaSolute = null;

        /// <summary>Constructor</summary>
        public OutputLayers()
        {
            NO3Solute = this.FindInScope("NO3") as ISolute;
            NH4Solute = this.FindInScope("NH4") as ISolute;
            UreaSolute = this.FindInScope("Urea") as ISolute;
        }

        /// <summary>Gets or sets the thickness of each layer.</summary>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        ///<summary>Gets the current soil water content of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] SW
        {
            get { return Layers.MapConcentration(waterBalance.SW, soilPhysical.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the current soil water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] SWmm
        {
            get { return Layers.MapMass(waterBalance.SWmm, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] PAW
        {
            get { return Layers.MapConcentration(waterBalance.PAW, soilPhysical.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] PAWmm
        {
            get { return Layers.MapMass(waterBalance.PAWmm, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the lower limit of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] LL15
        {
            get { return Layers.MapConcentration(soilPhysical.LL15, soilPhysical.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the lower limit of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] LL15mm
        {
            get { return Layers.MapMass(soilPhysical.LL15mm, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the upper limit of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] DUL
        {
            get { return Layers.MapConcentration(soilPhysical.DUL, soilPhysical.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the upper limit of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] DULmm
        {
            get { return Layers.MapMass(soilPhysical.DULmm, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at saturation of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] SAT
        {
            get { return Layers.MapConcentration(soilPhysical.SAT, soilPhysical.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at saturation of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] SATmm
        {
            get { return Layers.MapMass(soilPhysical.SATmm, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil urea N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] Urea
        {
            get { return Layers.MapMass(UreaSolute.kgha, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil ammonium N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] NH4
        {
            get { return Layers.MapMass(NH4Solute.kgha, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil nitrate N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] NO3
        {
            get { return Layers.MapMass(NO3Solute.kgha, soilPhysical.Thickness, Thickness); }
        }

        ///<summary>Gets the soil organic carbon content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("%")]
        public double[] OC
        {
            get { return Layers.MapConcentration(initial.OC, soilPhysical.Thickness, Thickness, double.NaN); }
        }
    }
}
