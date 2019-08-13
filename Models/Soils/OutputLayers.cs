namespace Models.Soils
{
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils.Standardiser;
    using System;
    using System.Xml.Serialization;

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
        [Link]
        private Soil Soil = null;

        private ISolute NO3Solute = null;
        private ISolute NH4Solute = null;
        private ISolute UreaSolute = null;

        /// <summary>Constructor</summary>
        public OutputLayers()
        {
            NO3Solute = Apsim.Find(this, "NO3") as ISolute;
            NH4Solute = Apsim.Find(this, "NH4") as ISolute;
            UreaSolute = Apsim.Find(this, "Urea") as ISolute;
        }

        /// <summary>Gets or sets the thickness of each layer.</summary>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        ///<summary>Gets the current soil water content of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] SW
        {
            get { return Layers.MapConcentration(Soil.SoilWater.SW, Soil.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the current soil water amount of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] SWmm
        {
            get { return Layers.MapMass(Soil.SoilWater.SWmm, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] PAW
        {
            get { return Layers.MapMass(Soil.PAW, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the lower limit of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] LL15
        {
            get { return Layers.MapConcentration(Soil.LL15, Soil.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the lower limit of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] LL15mm
        {
            get { return Layers.MapMass(Soil.LL15mm, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the upper limit of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] DUL
        {
            get { return Layers.MapConcentration(Soil.DUL, Soil.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the upper limit of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] DULmm
        {
            get { return Layers.MapMass(Soil.DULmm, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at saturation of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] SAT
        {
            get { return Layers.MapConcentration(Soil.SAT, Soil.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at saturation of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] SATmm
        {
            get { return Layers.MapMass(Soil.SATmm, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil urea N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] Urea
        {
            get { return Layers.MapMass(UreaSolute.kgha, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil ammonium N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] NH4
        {
            get { return Layers.MapMass(NH4Solute.kgha, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil nitrate N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] NO3
        {
            get { return Layers.MapMass(NO3Solute.kgha, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil organic carbon content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("%")]
        public double[] OC
        {
            get { return Layers.MapConcentration(Soil.Initial.OC, Soil.Thickness, Thickness, double.NaN); }
        }
    }
}
