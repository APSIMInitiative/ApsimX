using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Soils
{
    /// <summary>
    /// This class captures data from a soil analysis
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class Analysis : Model
    {
        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        /// <value>The depth.</value>
        [Summary]
        [Description("Depth")]
        [XmlIgnore]
        [Units("cm")]
        public string[] Depth
        {
            get
            {
                return Soil.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = Soil.ToThickness(value);
            }
        }

        /// <summary>Gets or sets the rocks.</summary>
        /// <value>The rocks.</value>
        [Description("Rocks")]
        [Units("%")]
        public double[] Rocks { get; set; }
        /// <summary>Gets or sets the rocks metadata.</summary>
        /// <value>The rocks metadata.</value>
        public string[] RocksMetadata { get; set; }
        /// <summary>Gets or sets the texture.</summary>
        /// <value>The texture.</value>
        [Description("Texture")]
        public string[] Texture { get; set; }
        /// <summary>Gets or sets the texture metadata.</summary>
        /// <value>The texture metadata.</value>
        public string[] TextureMetadata { get; set; }
        /// <summary>Gets or sets the munsell colour.</summary>
        /// <value>The munsell colour.</value>
        [Description("Munsell Colour")]
        public string[] MunsellColour { get; set; }
        /// <summary>Gets or sets the munsell metadata.</summary>
        /// <value>The munsell metadata.</value>
        public string[] MunsellMetadata { get; set; }

        /// <summary>Gets or sets the ec.</summary>
        /// <value>The ec.</value>
        [Summary]
        [Description("EC")]
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }
        /// <summary>Gets or sets the ec metadata.</summary>
        /// <value>The ec metadata.</value>
        public string[] ECMetadata { get; set; }

        /// <summary>Gets or sets the ph.</summary>
        /// <value>The ph.</value>
        [Summary]
        [Description("PH")]
        [Display(Format="N1")]
        public double[] PH { get; set; }
        /// <summary>Gets or sets the ph metadata.</summary>
        /// <value>The ph metadata.</value>
        public string[] PHMetadata { get; set; }

        /// <summary>Gets or sets the cl.</summary>
        /// <value>The cl.</value>
        [Summary]
        [Description("CL")]
        [Units("mg/kg")]
        public double[] CL { get; set; }
        /// <summary>Gets or sets the cl metadata.</summary>
        /// <value>The cl metadata.</value>
        public string[] CLMetadata { get; set; }
        /// <summary>Gets or sets the boron.</summary>
        /// <value>The boron.</value>
        [Description("Boron")]
        [Units("Hot water mg/kg")]
        public double[] Boron { get; set; }
        /// <summary>Gets or sets the boron metadata.</summary>
        /// <value>The boron metadata.</value>
        public string[] BoronMetadata { get; set; }
        /// <summary>Gets or sets the cec.</summary>
        /// <value>The cec.</value>
        [Description("CEC")]
        [Units("cmol+/kg")]
        public double[] CEC { get; set; }
        /// <summary>Gets or sets the cec metadata.</summary>
        /// <value>The cec metadata.</value>
        public string[] CECMetadata { get; set; }
        /// <summary>Gets or sets the ca.</summary>
        /// <value>The ca.</value>
        [Description("Ca")]
        [Units("cmol+/kg")]
        public double[] Ca { get; set; }
        /// <summary>Gets or sets the ca metadata.</summary>
        /// <value>The ca metadata.</value>
        public string[] CaMetadata { get; set; }
        /// <summary>Gets or sets the mg.</summary>
        /// <value>The mg.</value>
        [Description("Mg")]
        [Units("cmol+/kg")]
        public double[] Mg { get; set; }
        /// <summary>Gets or sets the mg metadata.</summary>
        /// <value>The mg metadata.</value>
        public string[] MgMetadata { get; set; }
        /// <summary>Gets or sets the na.</summary>
        /// <value>The na.</value>
        [Description("Na")]
        [Units("cmol+/kg")]
        public double[] Na { get; set; }
        /// <summary>Gets or sets the na metadata.</summary>
        /// <value>The na metadata.</value>
        public string[] NaMetadata { get; set; }
        /// <summary>Gets or sets the k.</summary>
        /// <value>The k.</value>
        [Description("K")]
        [Units("cmol+/kg")]
        public double[] K { get; set; }
        /// <summary>Gets or sets the k metadata.</summary>
        /// <value>The k metadata.</value>
        public string[] KMetadata { get; set; }

        /// <summary>Gets or sets the esp.</summary>
        /// <value>The esp.</value>
        [Summary]
        [Description("ESP")]
        [Units("%")]
        public double[] ESP { get; set; }
        /// <summary>Gets or sets the esp metadata.</summary>
        /// <value>The esp metadata.</value>
        public string[] ESPMetadata { get; set; }
        /// <summary>Gets or sets the mn.</summary>
        /// <value>The mn.</value>
        [Description("Mn")]
        [Units("mg/kg")]
        public double[] Mn { get; set; }
        /// <summary>Gets or sets the mn metadata.</summary>
        /// <value>The mn metadata.</value>
        public string[] MnMetadata { get; set; }
        /// <summary>Gets or sets the al.</summary>
        /// <value>The al.</value>
        [Description("Al")]
        [Units("cmol+/kg")]
        public double[] Al { get; set; }
        /// <summary>Gets or sets the al metadata.</summary>
        /// <value>The al metadata.</value>
        public string[] AlMetadata { get; set; }

        /// <summary>Gets or sets the particle size sand.</summary>
        /// <value>The particle size sand.</value>
        [Summary]
        [Description("Particle size sand")]
        [Units("%")]
        public double[] ParticleSizeSand { get; set; }
        /// <summary>Gets or sets the particle size sand metadata.</summary>
        /// <value>The particle size sand metadata.</value>
        public string[] ParticleSizeSandMetadata { get; set; }
        /// <summary>Gets or sets the particle size silt.</summary>
        /// <value>The particle size silt.</value>
        [Summary]
        [Description("Particle size silt")]
        [Units("%")]
        public double[] ParticleSizeSilt { get; set; }
        /// <summary>Gets or sets the particle size silt metadata.</summary>
        /// <value>The particle size silt metadata.</value>
        public string[] ParticleSizeSiltMetadata { get; set; }
        /// <summary>Gets or sets the particle size clay.</summary>
        /// <value>The particle size clay.</value>
        [Summary]
        [Description("Particle size clay")]
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }
        /// <summary>Gets or sets the particle size clay metadata.</summary>
        /// <value>The particle size clay metadata.</value>
        public string[] ParticleSizeClayMetadata { get; set; }

        // Support for PH units.
        /// <summary>
        /// An enumerated type for ph units
        /// </summary>
        public enum PHUnitsEnum 
        {
            /// <summary>water</summary>
            Water,

            /// <summary>CaCl2</summary>
            CaCl2 
        }
        /// <summary>Gets or sets the ph units.</summary>
        /// <value>The ph units.</value>
        public PHUnitsEnum PHUnits { get; set; }
        /// <summary>Phes the units to string.</summary>
        /// <param name="Units">The units.</param>
        /// <returns></returns>
        public string PHUnitsToString(PHUnitsEnum Units)
        {
            if (Units == PHUnitsEnum.CaCl2)
                return "CaCl2";
            return "1:5 water";
        }
        /// <summary>Phes the units set.</summary>
        /// <param name="ToUnits">To units.</param>
        public void PHUnitsSet(PHUnitsEnum ToUnits)
        {
            if (ToUnits != PHUnits)
            {
                // convert the numbers
                if (ToUnits == PHUnitsEnum.Water)
                {
                    // pH in water = (pH in CaCl X 1.1045) - 0.1375
                    PH = Utility.Math.Subtract_Value(Utility.Math.Multiply_Value(PH, 1.1045), 0.1375);
                }
                else
                {
                    // pH in CaCl = (pH in water + 0.1375) / 1.1045
                    PH = Utility.Math.Divide_Value(Utility.Math.AddValue(PH, 0.1375), 1.1045);
                }
                PHUnits = ToUnits;
            }


        }
        
        // Support for Boron units.
        /// <summary>
        /// 
        /// </summary>
        public enum BoronUnitsEnum { HotWater, HotCaCl2 }
        /// <summary>Gets or sets the boron units.</summary>
        /// <value>The boron units.</value>
        public BoronUnitsEnum BoronUnits { get; set; }
        /// <summary>Borons the units to string.</summary>
        /// <param name="Units">The units.</param>
        /// <returns></returns>
        public string BoronUnitsToString(BoronUnitsEnum Units)
        {
            if (Units == BoronUnitsEnum.HotCaCl2)
                return "Hot CaCl2";
            return "Hot water mg/kg";
        }
        /// <summary>Borons the units set.</summary>
        /// <param name="ToUnits">To units.</param>
        public void BoronUnitsSet(BoronUnitsEnum ToUnits)
        {
            BoronUnits = ToUnits;
        }


        /// <summary>PH. Units: (1:5 water)</summary>
        /// <value>The ph water.</value>
        public double[] PHWater
        {
            get
            {
                if (PHUnits == PHUnitsEnum.CaCl2)
                {
                    // pH in water = (pH in CaCl X 1.1045) - 0.1375
                    return Utility.Math.Subtract_Value(Utility.Math.Multiply_Value(PH, 1.1045), 0.1375);
                }
                else
                    return PH;
            }
        }

    }
}
