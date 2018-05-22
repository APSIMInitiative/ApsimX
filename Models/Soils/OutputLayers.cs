using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{
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

        /// <summary>Gets the depth boundaries of each layer.</summary>
        [XmlIgnore]
        [Units("cm")]
        [Caption("Depth")]
        [Description("Soil layer depth positions")]
        public string[] Depth { get { return Soil.ToDepthStrings(Thickness); } }
            
        /// <summary>Gets or sets the thickness of each layer.</summary>
        [Units("mm")]
        [Caption("Thickness")]
        [Description("Thickness for each soil layer")]
        public double[] Thickness { get; set; }

        ///<summary>Gets the current soil water content of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] SW
        {
            get { return Soil.Map(Soil.SoilWater.SW, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the current soil water amount of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] SWmm
        {
            get { return Soil.Map(Soil.SoilWater.SWmm, Soil.Thickness, Thickness,Soil.MapType.Mass); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] PAW
        {
            get { return Soil.Map(Soil.PAW, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil water content at the lower limit of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] LL15
        {
            get { return Soil.Map(Soil.LL15, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water amount at the lower limit of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] LL15mm
        {
            get { return Soil.Map(Soil.LL15mm, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil water content at the upper limit of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] DUL
        {
            get { return Soil.Map(Soil.DUL, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water amount at the upper limit of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] DULmm
        {
            get { return Soil.Map(Soil.DULmm, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil water content at saturation of each mapped layer</summary>
        [XmlIgnore]
        [Units("mm/mm")]
        public double[] SAT
        {
            get { return Soil.Map(Soil.SAT, Soil.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water amount at saturation of each mapped layer.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] SATmm
        {
            get { return Soil.Map(Soil.SATmm, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil urea N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] Urea
        {
            get { return Soil.Map(Soil.UreaN, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil ammonium N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] NH4
        {
            get { return Soil.Map(Soil.NH4N, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil nitrate N content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] NO3
        {
            get { return Soil.Map(Soil.NO3N, Soil.Thickness, Thickness, Soil.MapType.Mass); }
        }

        ///<summary>Gets the soil organic carbon content of each mapped layer.</summary>
        [XmlIgnore]
        [Units("%")]
        public double[] OC
        {
            get { return Soil.Map(Soil.OC, Soil.Thickness, Thickness); }
        }
    }
}
