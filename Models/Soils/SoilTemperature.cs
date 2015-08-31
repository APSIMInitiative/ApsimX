using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing soil temperature parameters
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(typeof(Soil))]
    public class SoilTemperature : Model
    {
        /// <summary>Gets or sets the boundary layer conductance.</summary>
        /// <value>The boundary layer conductance.</value>
        [Description("Boundary layer conductance")]
        [Units("(W/m2/K)")]
        public double BoundaryLayerConductance { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the initial soil temperature.</summary>
        /// <value>The initial soil temperature.</value>
        [Description("Initial soil temperature")]
        [Units("oC")]
        public double[] InitialSoilTemperature { get; set; }
    }

}


      