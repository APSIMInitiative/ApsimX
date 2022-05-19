namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Newtonsoft.Json;
    using System;

    /// <summary>This class captures chemical soil data</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Chemical : Model
    {
        /// <summary>An enumeration for specifying PH units.</summary>
        public enum PHUnitsEnum
        {
            /// <summary>PH as water method.</summary>
            [Description("1:5 water")]
            Water,

            /// <summary>PH as Calcium chloride method.</summary>
            [Description("CaCl2")]
            CaCl2
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("cm")]
        [JsonIgnore]
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

        /// <summary>pH</summary>
        [Summary]
        [Description("PH")]
        [Display(Format = "N1")]
        public double[] PH { get; set; }

        /// <summary>The units of pH.</summary>
        public PHUnitsEnum PHUnits { get; set; }

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

        /// <summary>EC metadata</summary>
        public string[] ECMetadata { get; set; }

        /// <summary>CL metadata</summary>
        public string[] CLMetadata { get; set; }

        /// <summary>ESP metadata</summary>
        public string[] ESPMetadata { get; set; }

        /// <summary>PH metadata</summary>
        public string[] PHMetadata { get; set; }
    }
}
