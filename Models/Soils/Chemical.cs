namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using Models.Core;
    using System;

    /// <summary>This class captures chemical soil data</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Chemical : Model
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

        /// <summary>Thickness of each layer.</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Description("NO3N")]
        [Summary]
        [Units("ppm")]
        public double[] NO3N { get; set; }

        /// <summary>Ammonia NH4</summary>
        [Description("NH4N")]
        [Summary]
        [Units("ppm")]
        public double[] NH4N { get; set; }

        /// <summary>pH</summary>
        [Summary]
        [Description("PH")]
        [Display(Format = "N1")]
        public double[] PH { get; set; }

        /// <summary>Gets or sets the cl.</summary>
        [Summary]
        [Description("CL")]
        [Units("mg/kg")]
        public double[] CL { get; set; }

        /// <summary>Gets or sets the ec.</summary>
        [Summary]
        [Description("EC")]
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }

        /// <summary>Gets or sets the esp.</summary>
        [Summary]
        [Description("ESP")]
        [Units("%")]
        public double[] ESP { get; set; }
    }
}
