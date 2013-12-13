using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Soils
{
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class Analysis : Model
    {
        [UserInterfaceIgnore]
        public double[] Thickness { get; set; }

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

        [Units("%")]
        public double[] Rocks { get; set; }
        public string[] RocksMetadata { get; set; }
        public string[] Texture { get; set; }
        public string[] TextureMetadata { get; set; }
        public string[] MunsellColour { get; set; }
        public string[] MunsellMetadata { get; set; }
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }
        public string[] ECMetadata { get; set; }
        public double[] PH { get; set; }
        public string[] PHMetadata { get; set; }
        [Units("mg/kg")]
        public double[] CL { get; set; }
        public string[] CLMetadata { get; set; }
        [Units("Hot water mg/kg")]
        public double[] Boron { get; set; }
        public string[] BoronMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] CEC { get; set; }
        public string[] CECMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Ca { get; set; }
        public string[] CaMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Mg { get; set; }
        public string[] MgMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Na { get; set; }
        public string[] NaMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] K { get; set; }
        public string[] KMetadata { get; set; }
        [Units("%")]
        public double[] ESP { get; set; }
        public string[] ESPMetadata { get; set; }
        [Units("mg/kg")]
        public double[] Mn { get; set; }
        public string[] MnMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Al { get; set; }
        public string[] AlMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeSand { get; set; }
        public string[] ParticleSizeSandMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeSilt { get; set; }
        public string[] ParticleSizeSiltMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }
        public string[] ParticleSizeClayMetadata { get; set; }

        // Support for PH units.
        public enum PHUnitsEnum { Water, CaCl2 }
        public PHUnitsEnum PHUnits { get; set; }
        public string PHUnitsToString(PHUnitsEnum Units)
        {
            if (Units == PHUnitsEnum.CaCl2)
                return "CaCl2";
            return "1:5 water";
        }
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
                    PH = Utility.Math.Divide_Value(Utility.Math.Add_Value(PH, 0.1375), 1.1045);
                }
                PHUnits = ToUnits;
            }


        }
        
        // Support for Boron units.
        public enum BoronUnitsEnum { HotWater, HotCaCl2 }
        public BoronUnitsEnum BoronUnits { get; set; }
        public string BoronUnitsToString(BoronUnitsEnum Units)
        {
            if (Units == BoronUnitsEnum.HotCaCl2)
                return "Hot CaCl2";
            return "Hot water mg/kg";
        }
        public void BoronUnitsSet(BoronUnitsEnum ToUnits)
        {
            BoronUnits = ToUnits;
        }


        /// <summary>
        /// PH. Units: (1:5 water)
        /// </summary>
        [UserInterfaceIgnore]
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
