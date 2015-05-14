using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A below ground organ
    /// </summary>
    [Serializable]
    public class BelowGroundOrgan : GenericOrgan, BelowGround, Reproductive
    {
        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Execute Harvesting logic for below ground organ
        /// </summary>
        public override void DoHarvest()
        {
                DateTime Today = new DateTime(Clock.Today.Year, 1, 1);
                Today = Today.AddDays(Clock.Today.Day - 1);
                double YieldDW = (Live.Wt + Dead.Wt);

                string message = "Harvesting " + Name + " from " + Plant.Name + "\r\n" +
                                 "  Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)";
                Summary.WriteMessage(this, message);

                Live.Clear();
                Dead.Clear();
        }
    }
}
