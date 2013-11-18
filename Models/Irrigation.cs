using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models
{
    /// <summary>
    /// Irrigation class.
    /// </summary>
    public class Irrigation : Model
    {
        // Links
        [Link] private ISummary Summary = null;

        // Output variables.
        public double IrrigationApplied { get; set; }

        // Events we're going to send.
        public event EventHandler<Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType> Irrigated;

        /// <summary>
        /// Apply irrigatopm.
        /// </summary>
        public void Apply(double amount, double depth = 0.0, double efficiency = 100.0, bool willRunoff = false)
        {
            if (Irrigated != null && amount > 0)
            {
                Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType water = new Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType();
                water.Amount = amount * efficiency;
                water.Depth = depth;
                water.will_runoff = willRunoff;
                IrrigationApplied = amount;
                Irrigated.Invoke(this, water);
                Summary.WriteMessage(string.Format("{0} mm of water added at depth {1}", amount, depth));

            }
        }

    }
}
