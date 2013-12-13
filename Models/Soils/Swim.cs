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
    public class Swim : Model
    {
        [Description("Bare soil albedo")]
        public double Salb { get; set; }
        [Description("Bare soil runoff curve number")]
        public double CN2Bare { get; set; }
        [Description("Max. reduction in curve number due to cover")]
        public double CNRed { get; set; }
        [Description("Cover for max curve number reduction")]
        public double CNCov { get; set; }
        [Description("Hydraulic conductivity at DUL (mm/d)")]
        public double KDul { get; set; }
        [Description("Matric Potential at DUL (cm)")]
        public double PSIDul { get; set; }
        [Description("Vapour Conductivity Calculations?")]
        public bool VC { get; set; }
        [Description("Minimum Timestep (min)")]
        public double DTmin { get; set; }
        [Description("Maximum Timestep (min)")]
        public double DTmax { get; set; }
        [Description("Maximum water increment (mm)")]
        public double MaxWaterIncrement { get; set; }
        [Description("Space weighting factor")]
        public double SpaceWeightingFactor { get; set; }
        [Description("Solute space weighting factor")]
        public double SoluteSpaceWeightingFactor { get; set; }
        [Description("Diagnostic Information?")]
        public bool Diagnostics { get; set; }

        public SwimSoluteParameters SwimSoluteParameters { get; set; }
        public SwimWaterTable SwimWaterTable { get; set; }
        public SwimSubsurfaceDrain SwimSubsurfaceDrain { get; set; }
    }

}
