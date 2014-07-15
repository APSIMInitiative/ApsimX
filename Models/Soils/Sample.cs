using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class Sample : Model
    {
        public string Date { get; set; }

        public double[] Thickness { get; set; }

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

        [Description("NO3")]
        public double[] NO3 { get; set; }
        [Description("NH4")]
        public double[] NH4 { get; set; }
        [Description("SW")]
        public double[] SW { get; set; }
        [Description("OC")]
        public double[] OC { get; set; }
        [Description("EC")]
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }
        [Description("CL")]
        [Units("mg/kg")]
        public double[] CL { get; set; }
        [Description("ESP")]
        [Units("%")]
        public double[] ESP { get; set; }
        [Description("PH")]
        public double[] PH { get; set; }

        public Sample() {Name = "Sample"; }

        // Support for NO3 units.
        public enum NUnitsEnum { ppm, kgha }
        public NUnitsEnum NO3Units { get; set; }
        public string NO3UnitsToString(NUnitsEnum Units)
        {
            if (Units == NUnitsEnum.kgha)
                return "kg/ha";
            return "ppm";
        }
        public void NO3UnitsSet(NUnitsEnum ToUnits, Soil Soil)
        {
            if (ToUnits != NO3Units)
            {
                // convert the numbers
                if (ToUnits == NUnitsEnum.ppm)
                    NO3 = NO3ppm(Soil);
                else
                    NO3 = NO3kgha(Soil);
                NO3Units = ToUnits;
            }
        }
        
        // Support for NH4 units.
        public NUnitsEnum NH4Units { get; set; }
        public string NH4UnitsToString(NUnitsEnum Units)
        {
            if (Units == NUnitsEnum.kgha)
                return "kg/ha";
            return "ppm";
        }
        public void NH4UnitsSet(NUnitsEnum ToUnits, Soil Soil)
        {
            double[] BD = Soil.BDMapped(Thickness);

            if (ToUnits != NH4Units)
            {
                // convert the numbers
                if (ToUnits == NUnitsEnum.ppm)
                    NH4 = NH4ppm(Soil);
                else
                    NH4 = NH4kgha(Soil);
                NH4Units = ToUnits;
            }
        }
        
        // Support for SW units.
        public enum SWUnitsEnum { Volumetric, Gravimetric, mm }
        public SWUnitsEnum SWUnits { get; set; }
        public string SWUnitsToString(SWUnitsEnum Units)
        {
            if (Units == SWUnitsEnum.Gravimetric)
                return "grav. mm/mm";
            if (Units == SWUnitsEnum.Volumetric)
                return "mm/mm";
            return "mm";
        }
        public void SWUnitsSet(SWUnitsEnum ToUnits, Soil Soil)
        {
            if (ToUnits != SWUnits)
            {
                // convert the numbers
                if (SWUnits == SWUnitsEnum.Volumetric)
                {
                    if (ToUnits == SWUnitsEnum.Gravimetric)
                        SW = Utility.Math.Divide(SW, Soil.BDMapped(Thickness));
                    else if (ToUnits == SWUnitsEnum.mm)
                        SW = Utility.Math.Multiply(SW, Thickness);
                }
                else if (SWUnits == SWUnitsEnum.Gravimetric)
                {
                    if (ToUnits == SWUnitsEnum.Volumetric)
                        SW = Utility.Math.Multiply(SW, Soil.BDMapped(Thickness));
                    else if (ToUnits == SWUnitsEnum.mm)
                        SW = Utility.Math.Multiply(Utility.Math.Multiply(SW, Soil.BDMapped(Thickness)), Thickness);
                }
                else
                {
                    if (ToUnits == SWUnitsEnum.Volumetric)
                        SW = Utility.Math.Divide(SW, Thickness);
                    else if (ToUnits == SWUnitsEnum.Gravimetric)
                        SW = Utility.Math.Divide(Utility.Math.Divide(SW, Thickness), Soil.BDMapped(Thickness));
                }
                SWUnits = ToUnits;
            }
        }

        // Support for OC units.
        public enum OCSampleUnitsEnum { Total, WalkleyBlack }
        public OCSampleUnitsEnum OCUnits { get; set; }
        public string OCUnitsToString(OCSampleUnitsEnum Units)
        {
            if (Units == OCSampleUnitsEnum.WalkleyBlack)
                return "Walkley Black %";
            return "Total %";
        }
        public void OCUnitsSet(OCSampleUnitsEnum ToUnits)
        {
            if (ToUnits != OCUnits)
            {
                // convert the numbers
                if (ToUnits == OCSampleUnitsEnum.WalkleyBlack)
                    OC = Utility.Math.Divide_Value(OC, 1.3);
                else
                    OC = Utility.Math.Multiply_Value(OC, 1.3);
                OCUnits = ToUnits;
            }
        }

        // Support for PH units.
        public enum PHSampleUnitsEnum { Water, CaCl2 }
        public PHSampleUnitsEnum PHUnits { get; set; }
        public string PHUnitsToString(PHSampleUnitsEnum Units)
        {
            if (Units == PHSampleUnitsEnum.CaCl2)
                return "CaCl2";
            return "1:5 water";
        }
        public void PHUnitsSet(PHSampleUnitsEnum ToUnits)
        {
            if (ToUnits != PHUnits)
            {
                // convert the numbers
                if (ToUnits == PHSampleUnitsEnum.Water)
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

        /// <summary>
        /// Return NO3. Units: ppm.
        /// </summary>
        public double[] NO3ppm(Soil Soil)
        {
            if (NO3 == null) return null;
            double[] NO3Values = (double[]) NO3.Clone();
            if (NO3Units != NUnitsEnum.ppm)
            {
                double[] BD = Soil.BDMapped(Thickness);
                for (int i = 0; i < NO3Values.Length; i++)
                {
                    if (NO3Values[i] != double.NaN)
                        NO3Values[i] = NO3Values[i] * 100 / (BD[i] * Thickness[i]);
                }
            }
                return NO3Values;
        }
        
        /// <summary>
        /// Return NO3. Units: kg/ha.
        /// </summary>
        public double[] NO3kgha(Soil Soil)
        {
            if (NO3 == null) return null;
            double[] NO3Values = (double[])NO3.Clone();
            if (NO3Units != NUnitsEnum.kgha)
            {
                double[] BD = Soil.BDMapped(Thickness);
                for (int i = 0; i < NO3Values.Length; i++)
                {
                    if (NO3Values[i] != double.NaN)
                        NO3Values[i] = NO3Values[i] / 100 * (BD[i] * Thickness[i]);
                }
            }
            return NO3Values;
        }
        
        /// <summary>
        /// Return NH4. Units: ppm.
        /// </summary>
        public double[] NH4ppm(Soil Soil)
        {
            if (NH4 == null) return null;
            double[] NH4Values = (double[])NH4.Clone();
            if (NH4Units != NUnitsEnum.ppm)
            {
                double[] BD = Soil.BDMapped(Thickness);
                for (int i = 0; i < NH4Values.Length; i++)
                {
                    if (NH4Values[i] != double.NaN)
                        NH4Values[i] = NH4Values[i] * 100 / (BD[i] * Thickness[i]);
                }
            }
            return NH4Values;
        }

        /// <summary>
        /// Return NH4. Units: kg/ha.
        /// </summary>
        public double[] NH4kgha(Soil Soil)
        {
            if (NH4 == null) return null;
            double[] NH4Values = (double[])NH4.Clone();
            if (NH4Units != NUnitsEnum.kgha)
            {
                double[] BD = Soil.BDMapped(Thickness);
                for (int i = 0; i < NH4Values.Length; i++)
                {
                    if (NH4Values[i] != double.NaN)
                        NH4Values[i] = NH4Values[i] / 100 * (BD[i] * Thickness[i]);
                }
            }
            return NH4Values;
        }


        /// <summary>
        /// Return SW. Units: vol mm/mm.
        /// </summary>
        public double[] SWVolumetric(Soil Soil)
        {
            if (SW == null) return null;
            double[] OriginalValues = (double[]) SW.Clone();
            SWUnitsSet(SWUnitsEnum.Volumetric, Soil);
            double[] Values = (double[]) SW.Clone();
            SW = OriginalValues;
            return Values;
        }


        /// <summary>
        /// Organic carbon. Units: Total %
        /// </summary>
        public double[] OCTotal
        {
            get
            {
                if (OCUnits == OCSampleUnitsEnum.WalkleyBlack)
                    return Utility.Math.Multiply_Value(OC, 1.3);
                else
                    return OC;
            }
        }

        /// <summary>
        /// PH. Units: (1:5 water)
        /// </summary>
        public double[] PHWater
        {
            get
            {
                if (PHUnits == PHSampleUnitsEnum.CaCl2)
                {
                    // pH in water = (pH in CaCl X 1.1045) - 0.1375
                    return Utility.Math.Subtract_Value(Utility.Math.Multiply_Value(PH, 1.1045), 0.1375);
                }
                else
                    return PH;
            }
        }

        internal static bool OverlaySampleOnTo(double[] SampleValues, double[] SampleThickness,
                                               ref double[] SoilValues, ref double[] SoilThickness)
        {
            if (Utility.Math.ValuesInArray(SampleValues))
            {
                double[] Values = (double[]) SampleValues.Clone();
                double[] Thicknesses = (double[]) SampleThickness.Clone();
                InFillValues(ref Values, ref Thicknesses, SoilValues, SoilThickness);
                SoilValues = Values;
                SoilThickness = Thicknesses;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Takes values from SoilValues and puts them at the bottom of SampleValues.  
        /// </summary>
        private static void InFillValues(ref double[] SampleValues, ref double[] SampleThickness,
                                         double[] SoilValues, double[] SoilThickness)
        {
            //-------------------------------------------------------------------------
            //  e.g. IF             SoilThickness  Values   SampleThickness	SampleValues
            //                           0-100		2         0-100				10
            //                         100-250	    3	     100-600			11
            //                         250-500		4		
            //                         500-750		5
            //                         750-900		6
            //						  900-1200		7
            //                        1200-1500		8
            //                        1500-1800		9
            //
            // will produce:		SampleThickness	        Values
            //						     0-100				  10
            //						   100-600				  11
            //						   600-750				   5
            //						   750-900				   6
            //						   900-1200				   7
            //						  1200-1500				   8
            //						  1500-1800				   9
            //
            //-------------------------------------------------------------------------
            if (SoilValues == null || SoilThickness == null) return;

            // remove missing layers.
            for (int i = 0; i < SampleValues.Length; i++)
            {
                if (double.IsNaN(SampleValues[i]) || double.IsNaN(SampleThickness[i]))
                {
                    SampleValues[i] = double.NaN;
                    SampleThickness[i] = double.NaN;
                }
            }
            SampleValues = Utility.Math.RemoveMissingValuesFromBottom(SampleValues);
            SampleThickness = Utility.Math.RemoveMissingValuesFromBottom(SampleThickness);

            double CumSampleDepth = Utility.Math.Sum(SampleThickness);

            //Work out if we need to create a dummy layer so that the sample depths line up 
            //with the soil depths
            double CumSoilDepth = 0.0;
            for (int SoilLayer = 0; SoilLayer < SoilThickness.Length; SoilLayer++)
            {
                CumSoilDepth += SoilThickness[SoilLayer];
                if (CumSoilDepth > CumSampleDepth)
                {
                    Array.Resize(ref SampleThickness, SampleThickness.Length + 1);
                    Array.Resize(ref SampleValues, SampleValues.Length + 1);
                    int i = SampleThickness.Length - 1;
                    SampleThickness[i] = CumSoilDepth - CumSampleDepth;
                    if (SoilValues[SoilLayer] == Utility.Math.MissingValue)
                        SampleValues[i] = 0.0;
                    else
                        SampleValues[i] = SoilValues[SoilLayer];
                    CumSampleDepth = CumSoilDepth;
                }
            }
        }
    }
}
