namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using Models.Core;
    using System;

    /// <summary>A model for capturing soil organic parameters</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Organic : Model
    {
        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("cm")]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Root C:N Ratio</summary>
        [Summary]
        [Description("FOM C:N ratio (0-500)")]
        [Bounds(Lower = 0.0, Upper = 500.0)]
        public double FOMCNRatio { get; set; }

        /// <summary>Soil layer thickness for each layer (mm)</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Carbon concentration (Total% 0.1 - 10%)</summary>
        [Summary]
        [Description("Organic Carbon")]
        [Bounds(Lower = 0.1, Upper = 10.0)]
        [Display(Format = "N2")]
        [Units("Total%")]
        public double[] Carbon { get; set; }

        /// <summary>Carbon:nitrogen ratio.</summary>
        [Summary]
        [Description("Soil C:N ratio")]
        [Units("g/g")]
        [Bounds(Lower = 5.0, Upper = 30.0)]
        public double[] SoilCNRatio { get; set; }

        /// <summary>F biom.</summary>
        [Summary]
        [Description("FBiom")]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FBiom { get; set; }

        /// <summary>F inert.</summary>
        [Summary]
        [Description("FInert")]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FInert { get; set; }

        /// <summary>Fresh organic matter</summary>
        [Summary]
        [Description("Fresh organic matter")]
        [Units("kg/ha")]
        [Display(Format = "N1")]
        public double[] FOM { get; set; }
    }
}