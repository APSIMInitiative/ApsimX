using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

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
        /// <summary>The depth boundaries of each layer</summary>
        /// <value>The thickness.</value>
        [XmlIgnore]
        [Units("cm")]
        [Caption("Depth")]
        [Description("Soil layer depth positions")]
        public string[] Depth
        {
            get
            {
                return Soil.ToDepthStrings(Thickness);
            }
        }
        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        [Units("mm")]
        [Caption("Thickness")]
        [Description("Soil layer thickness for each layer")]
        public double[] Thickness { get; set; }       
    }
}
