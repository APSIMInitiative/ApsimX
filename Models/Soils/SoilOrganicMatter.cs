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
    public class SoilOrganicMatter : Model
    {
        [Description("Root C:N ratio")]
        public double RootCN { get; set; }
        [Description("Root Weight (kg/ha)")]
        public double RootWt { get; set; }
        [Description("Soil C:N ratio")]
        public double SoilCN { get; set; }
        [Description("Erosion enrichment coefficient A")]
        public double EnrACoeff { get; set; }
        [Description("Erosion enrichment coefficient B")]
        public double EnrBCoeff { get; set; }

        public double[] Thickness { get; set; }

        [Units("cm")]
        [Description("Depth")]
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

        [Description("OC")]
        public double[] OC { get; set; }
        public string[] OCMetadata { get; set; }
        [Description("FBiom")]
        [Units("0-1")]
        public double[] FBiom { get; set; }

        [Description("FInert")]
        [Units("0-1")]
        public double[] FInert { get; set; }

        private const double ppm = 1000000.0;

        // Support for OC units.
        public enum OCUnitsEnum { Total, WalkleyBlack }
        public OCUnitsEnum OCUnits { get; set; }
        public string OCUnitsToString(OCUnitsEnum Units)
        {
            if (Units == OCUnitsEnum.WalkleyBlack)
                return "Walkley Black %";
            return "Total %";
        }
        public void OCUnitsSet(OCUnitsEnum ToUnits)
        {
            if (ToUnits != OCUnits)
            {
                // convert the numbers
                if (ToUnits == OCUnitsEnum.WalkleyBlack)
                    OC = Utility.Math.Divide_Value(OC, 1.3);
                else
                    OC = Utility.Math.Multiply_Value(OC, 1.3);
                OCUnits = ToUnits;
            }
        }


        /// <summary>
        /// Organic carbon. Units: Total %
        /// </summary>
        public double[] OCTotal
        {
            get
            {
                if (OCUnits == OCUnitsEnum.WalkleyBlack)
                    return Utility.Math.Multiply_Value(OC, 1.3);
                else
                    return OC;
            }
        }


        /// <summary>
        /// Calculate and return amount of inert carbon on the same layer structure as OC. Units: kg/ha
        /// </summary>
        [DisplayFormat("N0")]
        [Units("kg/ha")]
        public double[] InertC
        {
            get
            {
                Soil soil = Parent as Soil;
                if (soil != null)
                {
                    double[] BD = soil.BDMapped(Thickness);

                    double[] InertC = new double[Thickness.Length];

                    for (int i = 0; i < OC.Length; i++)
                    {
                        if (FInert[i] == double.NaN ||
                            OC[i] == double.NaN ||
                            BD[i] == double.NaN)
                            InertC[i] = double.NaN;
                        else
                        {
                            double soiln2_fac = 100.0 / (BD[i] * Thickness[i]);
                            double oc_ppm = OCTotal[i] / 100 * ppm;
                            double carbon_tot = oc_ppm / soiln2_fac;
                            InertC[i] = FInert[i] * carbon_tot;
                        }
                    }
                    return InertC;
                }
                return null;
            }
        }

        /// <summary>
        /// Calculate and return the amount of biom carbon on the same layer structure as OC. Units: kg/ha
        /// </summary>
        [DisplayFormat("N0")]
        [Units("kg/ha")]
        public double[] BiomC
        {
            get
            {
                Soil soil = Parent as Soil;
                if (soil != null)
                {
                    double[] BD = soil.BDMapped(Thickness);
                    double[] InertC = this.InertC;

                    double[] BiomC = new double[Thickness.Length];
                    for (int i = 0; i < Thickness.Length; i++)
                    {
                        if (i >= OC.Length ||
                            i >= FBiom.Length ||
                            i >= BD.Length ||
                            i >= InertC.Length ||
                            i >- BiomC.Length ||
                            OC[i] == double.NaN ||
                            FBiom[i] == double.NaN ||
                            BD[i] == double.NaN ||
                            InertC[i] == double.NaN)
                            BiomC[i] = double.NaN;
                        else
                        {
                            double soiln2_fac = 100.0 / (BD[i] * Thickness[i]);
                            double oc_ppm = OCTotal[i] / 100 * ppm;
                            double carbon_tot = oc_ppm / soiln2_fac;
                            BiomC[i] = ((carbon_tot - InertC[i]) * FBiom[i]) / (1.0 + FBiom[i]);
                        }
                    }
                    return BiomC;
                }
                return null;
            }
        }

        /// <summary>
        /// Calculate and return the amount of humic carbon on the same layer structure as OC. Units: kg/ha
        /// </summary>
        [DisplayFormat("N0")]
        [Units("kg/ha")]
        public double[] HumC
        {
            get
            {
                Soil soil = Parent as Soil;
                if (soil != null)
                {
                    double[] BD = soil.BDMapped(Thickness);
                    double[] InertC = this.InertC;
                    double[] BiomC = this.BiomC;

                    double[] HumC = new double[Thickness.Length];

                    for (int i = 0; i < Thickness.Length; i++)
                    {
                        if (BiomC[i] == double.NaN)
                            HumC[i] = double.NaN;
                        else
                        {
                            double soiln2_fac = 100.0 / (BD[i] * Thickness[i]);
                            double oc_ppm = OCTotal[i] / 100 * ppm;
                            double carbon_tot = oc_ppm / soiln2_fac;
                            HumC[i] = carbon_tot - BiomC[i] - InertC[i];
                        }
                    }
                    return HumC;
                }
                return null;
            }
        }
    }
}
