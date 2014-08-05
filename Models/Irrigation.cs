using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>
    /// Irrigation class.
    /// </summary>
    [Serializable]
    public class Irrigation : Model
    {
        // Links
        [Link] private ISummary Summary = null;

        // Output variables.
        [XmlIgnore]
        public double IrrigationApplied { get; set; }

        // Events we're going to send.
        public event EventHandler<Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType> Irrigated;

        /// <summary>
        /// Apply irrigatopm.
        /// </summary>
        public void Apply(double amount, double depth = 0.0, double efficiency = 1.0, bool willRunoff = false)
        {
            if (Irrigated != null && amount > 0)
            {
                if(efficiency > 1.0 || efficiency < 0)
                    throw new ApsimXException(FullPath, "Efficiency value for irrigation event must bet between 0 and 1 ");

                Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType water = new Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType();
                water.Amount = amount * efficiency;
                water.Depth = depth;
                water.will_runoff = willRunoff;
                IrrigationApplied = amount;
                Irrigated.Invoke(this, water);
                Summary.WriteMessage(FullPath, string.Format("{0} mm of water added at depth {1}", amount * efficiency, depth));
            }
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            IrrigationApplied = 0;
        }

    }
}
