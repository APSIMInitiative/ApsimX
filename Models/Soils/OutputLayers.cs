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
    /// This class takes soil water contents simulated at each of the water models layers and maps them onto the layering sepcified here so they can be compared with observed values
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class OutputLayers : Model
    {
        [Link]
        private Soil Soil = null;

        /// <summary>The depth boundaries of each layer</summary>
        /// <value>The thickness.</value>
        [XmlIgnore]
        [Units("mm")]
        [Description("Soil layer depth positions (cm)")]
        public string[] Depth { get { return Soil.ToDepthStrings(Thickness); } }
            
        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        [Units("mm")]
        [Description("Soil layer thickness for each layer (mm)")]
        public double[] Thickness { get; set; }

        ///<summary> Soil water content (ml/ml) of each layer mapped onto specified layering to match observations </summary>
        [XmlIgnore]
        public double[] SW { get { return Soil.Map(Soil.SoilWater.SW, Soil.Thickness, Thickness); } }
        
        ///<summary> Soil water content (mm) of each layer mapped onto specified layering to match observations</summary>
        [XmlIgnore]
        public double[] SWmm { get { return Soil.Map(Soil.SoilWater.SWmm, Soil.Thickness, Thickness,Soil.MapType.Mass); } }
        
        ///<summary> Plant available water content (ml/ml) of each layer mapped onto specified layering to match observations</summary>
        [XmlIgnore]
        public double[] PAW { get { return Soil.Map(Soil.PAW, Soil.Thickness, Thickness, Soil.MapType.Mass); } }

        ///<summary> Soil Nitrate content of each layer mapped onto specified layering to match observations</summary>
        [XmlIgnore]
        public double[] NO3 { get { return Soil.Map(Soil.NO3N, Soil.Thickness, Thickness, Soil.MapType.Mass); } }
        
        ///<summary> Soil Amonium content of each layer mapped onto specified layering to match observations</summary>
        [XmlIgnore]
        public double[] NH4 { get { return Soil.Map(Soil.NH4N, Soil.Thickness, Thickness, Soil.MapType.Mass); } }
    }
}
