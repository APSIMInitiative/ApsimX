using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Xml;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Soils
{
    /// <summary>
    /// A model for holding layer structure information
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class LayerStructure : Model
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


        /// <summary>Gets or sets the thickness.</summary>
        [Units("mm")]
        public double[] Thickness { get; set; }       
    }
}
