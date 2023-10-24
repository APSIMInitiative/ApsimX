using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Store of Ruminant energy for the time-step
    /// </summary>
    internal class RuminantEnergyInfo
    {
        private readonly Ruminant ruminant;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="individual">The ruminant this energy relates to</param>
        public RuminantEnergyInfo(Ruminant individual)
        {
            ruminant = individual;            
        }

        /// <summary>
        /// Energy obtained from intake
        /// </summary>
        public double FromIntake { get { return ruminant.Intake.ME; } }

        /// <summary>
        /// Energy used for maintenance
        /// </summary>
        public double ForMaintenance { get; set; }

        /// <summary>
        /// Energy used for fetal development
        /// </summary>
        public double ForFetus { get; set; }

        /// <summary>
        /// Energy available after accounting for pregnancy
        /// </summary>
        public double AfterPregnancy { get { return FromIntake - ForMaintenance - ForFetus; } }

        /// <summary>
        /// Energy used for milk production
        /// </summary>
        public double ForLactation { get; set; }

        /// <summary>
        /// Energy available after lactation demands
        /// </summary>
        public double AfterLactation { get { return AfterPregnancy - ForLactation; } }

        /// <summary>
        /// Energy used for wool production
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Energy available after wool demands
        /// </summary>
        public double AfterWool { get { return ForLactation - ForWool; } }

        /// <summary>
        /// Energy used for maintenance
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Energy available for growth
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <summary>
        /// Reset all running stores
        /// </summary>
        public void Reset()
        {
            ForMaintenance = 0;
            ForFetus = 0;
            ForLactation = 0;
            ForGain = 0;
            ForWool = 0;
        }

    }
}
