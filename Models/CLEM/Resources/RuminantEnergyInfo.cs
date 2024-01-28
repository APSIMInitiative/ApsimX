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
    [Serializable]
    public class RuminantEnergyInfo
    {
        private readonly RuminantIntake ruminantIntake;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="intake">The intake component of the parent ruminant</param>
        public RuminantEnergyInfo(RuminantIntake intake)
        {
            ruminantIntake = intake;
        }

        /// <summary>
        /// Energy obtained from intake
        /// </summary>
        public double FromIntake { get { return ruminantIntake.ME; } }

        /// <summary>
        /// Energy used for maintenance
        /// </summary>
        public double ForMaintenance { get; set; }

        /// <summary>
        /// Energy used for basal metabolism
        /// </summary>
        public double ForBasalMetabolism { get; set; }

        /// <summary>
        /// Energy used for HP Viscera
        /// </summary>
        public double ForHPViscera { get; set; }

        /// <summary>
        /// Energy used to move while grazing
        /// </summary>
        public double ToMove { get; set; }

        /// <summary>
        /// Energy used to graze while grazing
        /// </summary>
        public double ToGraze { get; set; }

        /// <summary>
        /// Total Energy used for grazing
        /// </summary>
        public double ForGrazing { get { return ToMove + ToGraze; } }

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
        /// Energy available for growth
        /// </summary>
        public double NetForGain { get; set; }

        /// <summary>
        /// Energy available for growth
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <summary>
        /// Energy used for protein
        /// </summary>
        public RuminantTrackingItem Protein { get; set; } = new();

        /// <summary>
        /// Energy used for fat
        /// </summary>
        public RuminantTrackingItem Fat { get; set; } = new();

        /// <summary>
        /// Reset all running stores
        /// </summary>
        public void Reset()
        {
            ForMaintenance = 0;
            ForBasalMetabolism = 0;
            ForHPViscera = 0;
            ForFetus = 0;
            ForLactation = 0;
            NetForGain = 0;
            ForWool = 0;
            ToMove = 0;
            ToGraze = 0;
        }

    }
}
