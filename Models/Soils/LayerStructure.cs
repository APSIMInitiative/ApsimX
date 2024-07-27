using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>A model for holding layer structure information</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class LayerStructure : Model
    {
        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Display]
        [Summary]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get => SoilUtilities.ToDepthStrings(Thickness);
            set => Thickness = SoilUtilities.ToThickness(value);
        }

        /// <summary>Gets or sets the thickness.</summary>
        public double[] Thickness { get; set; }
    }
}
