using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class SoilTemperature : Model
    {
        [Description("Boundary layer conductance")]
        [Units("(W/m2/K)")]
        public double BoundaryLayerConductance { get; set; }

        public double[] Thickness { get; set; }

        [Description("Initial soil temperature")]
        [Units("oC")]
        public double[] InitialSoilTemperature { get; set; }
    }

}


      