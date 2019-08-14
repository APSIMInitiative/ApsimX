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
    [ValidParent(ParentType=typeof(Soil))]
    public class SoilOrganicMatter : Model
    {
        /// <summary>Gets or sets the root cn.</summary>
        /// <value>The root cn.</value>
        [Summary]
        [Description("Root C:N ratio (0-500)")]
        [Bounds(Lower = 0.0, Upper = 500.0)]
        public double RootCN { get; set; }

        /// <summary>Gets or sets the enr a coeff.</summary>
        /// <value>The enr a coeff.</value>
        [Summary]
        [Description("Erosion enrichment coefficient A (1-20)")]
        [Bounds(Lower = 1.0, Upper = 20.0)]
        public double EnrACoeff { get; set; }

        /// <summary>Gets or sets the enr b coeff.</summary>
        /// <value>The enr b coeff.</value>
        [Summary]
        [Description("Erosion enrichment coefficient B (0-20)")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        public double EnrBCoeff { get; set; }

        /// <summary>Soil layer thickness for each layer (mm)</summary>
        /// <value>The thickness.</value>
        [Description("Depth (mm)")]
        [Summary]
        public double[] Thickness { get; set; }

        /// <summary>Organic carbon concentration (0.1 - 10%)</summary>
        /// <value>The oc.</value>
        [Summary]
        [Description("Organic Carbon")]
        [Bounds(Lower = 0.1, Upper = 10.0)]
        // Units may be either "Total %" or "Walkley Black %". We store this labelling information in OCUnitsEnum and don't need it here.
        // [Units("%")]
        [Display(Format = "N2")]
        public double[] OC { get; set; }
        /// <summary>Gets or sets the oc metadata.</summary>
        /// <value>The oc metadata.</value>
        public string[] OCMetadata { get; set; }

        /// <summary>Gets or sets the soil cn.</summary>
        /// <value>The soil cn.</value>
        [Summary]
        [Description("Soil C:N")]
        [Units("g/g")]
        [Bounds(Lower = 5.0, Upper = 30.0)]
        public double[] SoilCN { get; set; }

        /// <summary>Gets or sets the f biom.</summary>
        /// <value>The f biom.</value>
        [Summary]
        [Description("FBiom")]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FBiom { get; set; }

        /// <summary>Gets or sets the f inert.</summary>
        /// <value>The f inert.</value>
        [Summary]
        [Description("FInert")]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FInert { get; set; }

        /// <summary>Gets or sets the Initial Root Wt</summary>
        /// <value>The f inert.</value>
        [Summary]
        [Description("RootWt")]
        [Units("kg/ha")]
        [Display(Format = "N1")]
        public double[] RootWt { get; set; }

        /// <summary>The PPM</summary>
        private const double ppm = 1000000.0;

        /// <summary>Gets or sets the oc units.</summary>
        /// <value>The oc units.</value>
        public Sample.OCSampleUnitsEnum OCUnits { get; set; }

        /// <summary>Ocs the units set.</summary>
        /// <param name="ToUnits">To units.</param>
        public void OCUnitsSet(Sample.OCSampleUnitsEnum ToUnits)
        {
            if (ToUnits != OCUnits)
            {
                // convert the numbers
                if (ToUnits == Sample.OCSampleUnitsEnum.WalkleyBlack)
                    OC = MathUtilities.Divide_Value(OC, 1.3);
                else
                    OC = MathUtilities.Multiply_Value(OC, 1.3);
                OCUnits = ToUnits;
            }
        }


        /// <summary>Soil organic carbon</summary>
        /// <value>The oc total.</value>
        [Units("kg/ha")]
        public double[] OCTotal
        {
            get
            {
                if (OCUnits == Sample.OCSampleUnitsEnum.WalkleyBlack)
                    return MathUtilities.Multiply_Value(OC, 1.3);
                else
                    return OC;
            }
        }

    }
}
