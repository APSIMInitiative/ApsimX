using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using APSIM.Shared.Utilities;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing soil organic matter properties
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(typeof(Soil))]
    public class SoilOrganicMatter : Model
    {
        /// <summary>Gets or sets the root cn.</summary>
        /// <value>The root cn.</value>
        [Summary]
        [Description("Root C:N ratio")]
        public double RootCN { get; set; }

        /// <summary>Gets or sets the root wt.</summary>
        /// <value>The root wt.</value>
        [Summary]
        [Description("Root Weight (kg/ha)")]
        public double RootWt { get; set; }

        /// <summary>Gets or sets the soil cn.</summary>
        /// <value>The soil cn.</value>
        [Summary]
        [Description("Soil C:N ratio")]
        public double SoilCN { get; set; }

        /// <summary>Gets or sets the enr a coeff.</summary>
        /// <value>The enr a coeff.</value>
        [Summary]
        [Description("Erosion enrichment coefficient A")]
        public double EnrACoeff { get; set; }

        /// <summary>Gets or sets the enr b coeff.</summary>
        /// <value>The enr b coeff.</value>
        [Summary]
        [Description("Erosion enrichment coefficient B")]
        public double EnrBCoeff { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        /// <value>The depth.</value>
        [Summary]
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

        /// <summary>Gets or sets the oc.</summary>
        /// <value>The oc.</value>
        [Summary]
        [Description("OC")]
        [Display(Format = "N2")]
        public double[] OC { get; set; }
        /// <summary>Gets or sets the oc metadata.</summary>
        /// <value>The oc metadata.</value>
        public string[] OCMetadata { get; set; }

        /// <summary>Gets or sets the f biom.</summary>
        /// <value>The f biom.</value>
        [Summary]
        [Description("FBiom")]
        [Units("0-1")]
        public double[] FBiom { get; set; }

        /// <summary>Gets or sets the f inert.</summary>
        /// <value>The f inert.</value>
        [Summary]
        [Description("FInert")]
        [Units("0-1")]
        public double[] FInert { get; set; }

        /// <summary>The PPM</summary>
        private const double ppm = 1000000.0;

        // Support for OC units.
        /// <summary>
        /// 
        /// </summary>
        public enum OCUnitsEnum 
        {
            /// <summary>The total</summary>
            Total,

            /// <summary>The walkley black</summary>
            WalkleyBlack 
        }
        /// <summary>Gets or sets the oc units.</summary>
        /// <value>The oc units.</value>
        public OCUnitsEnum OCUnits { get; set; }
        /// <summary>Ocs the units to string.</summary>
        /// <param name="Units">The units.</param>
        /// <returns></returns>
        public string OCUnitsToString(OCUnitsEnum Units)
        {
            if (Units == OCUnitsEnum.WalkleyBlack)
                return "Walkley Black %";
            return "Total %";
        }
        /// <summary>Ocs the units set.</summary>
        /// <param name="ToUnits">To units.</param>
        public void OCUnitsSet(OCUnitsEnum ToUnits)
        {
            if (ToUnits != OCUnits)
            {
                // convert the numbers
                if (ToUnits == OCUnitsEnum.WalkleyBlack)
                    OC = MathUtilities.Divide_Value(OC, 1.3);
                else
                    OC = MathUtilities.Multiply_Value(OC, 1.3);
                OCUnits = ToUnits;
            }
        }


        /// <summary>Organic carbon. Units: Total %</summary>
        /// <value>The oc total.</value>
        public double[] OCTotal
        {
            get
            {
                if (OCUnits == OCUnitsEnum.WalkleyBlack)
                    return MathUtilities.Multiply_Value(OC, 1.3);
                else
                    return OC;
            }
        }


        /// <summary>
        /// Calculate and return amount of inert carbon on the same layer structure as OC. Units: kg/ha
        /// </summary>
        /// <value>The inert c.</value>
        [Display(Format = "N0")]
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
        /// <value>The biom c.</value>
        [Display(Format = "N0")]
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
        /// <value>The hum c.</value>
        [Display(Format = "N0")]
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
