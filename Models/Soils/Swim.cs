using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Models.Core;
using Models;
using System.Xml.Serialization;

namespace Models.Soils
{
    ///<summary>
    /// .NET port of the Fortran SWIM3 model
    /// Ported by Eric Zurcher July-August 2014
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class Swim3 : Model
    {
        #region Links

        [Link]
        private Clock Clock = null;

        //[Link]
        //private Component My = null;  // Get access to "Warning" function

        [Link]
        WeatherFile MetFile;

        [Link]
        Simulation Paddock;

        [Link]
        private Soil Soil = null;
       
        #endregion

        #region Constants

        const double effpar = 0.184;
        const double psi_ll15 = -15000.0;
        const double psiad = -1e6;
        const double psi0 = -0.6e7;

        #endregion


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
