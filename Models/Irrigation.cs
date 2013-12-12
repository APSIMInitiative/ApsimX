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
        public void Apply(double Amount, double Depth = 0.0, double Efficiency = 1.0, bool WillRunoff = false)
        {
            if (Irrigated != null && Amount > 0)
            {
                if(Efficiency > 1.0 || Efficiency < 0)
                    throw new ApsimXException(FullPath, "Efficiency value for irrigation event must bet between 0 and 1 ");

                Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType water = new Models.SurfaceOM.SurfaceOrganicMatter.IrrigationApplicationType();
                water.Amount = Amount * Efficiency;
                water.Depth = Depth;
                water.will_runoff = WillRunoff;
                IrrigationApplied = Amount;
                Irrigated.Invoke(this, water);
                Summary.WriteMessage(FullPath, string.Format("{0} mm of water added at depth {1}", Amount * Efficiency, Depth));
            }
        }

    }
}
