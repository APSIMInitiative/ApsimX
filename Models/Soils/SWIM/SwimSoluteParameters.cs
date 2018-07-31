using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing swim solute parameters
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Swim3))]
    public class SwimSoluteParameters : Model
    {
        /// <summary>Concentration of solute in water table (ppm).</summary>
        [Description("Concentration of solute in water table (ppm).")]
        public double WaterTableConcentration { get; set; }

        /// <summary>Gets or sets the diffusion coefficient (D0).</summary>
        [Description("D0")]
        public double D0 { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the exco.</summary>
        [Description("Exco")]
        public double[] Exco { get; set; }

        /// <summary>Gets or sets the fip.</summary>
        [Description("FIP")]
        public double[] FIP { get; set; }

    }

}
