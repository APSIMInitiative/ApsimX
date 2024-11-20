using DocumentFormat.OpenXml.Vml.Office;
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
    public class RuminantInfoEnergy
    {
        private readonly RuminantIntake ruminantIntake;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="intake">The intake component of the parent ruminant</param>
        /// <param name="initialAmount">Initial MJ to set</param>
        public RuminantInfoEnergy(RuminantIntake intake, double initialAmount = 0)
        {
            ruminantIntake = intake;
        }

        /// <summary>
        /// Energy obtained from intake
        /// </summary>
        public double FromIntake { get { return ruminantIntake.ME; } }

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
        /// Energy used for maintenance
        /// </summary>
        public double ForMaintenance { get { return ForBasalMetabolism + ForHPViscera + ForGrazing; } }

        /// <summary>
        /// Energetic cost of depsoiting protein and fat. Heat for product formation 
        /// </summary>
        public double ForProductFormation { get; set; } = 0.0;

        /// <summary>
        /// Averaged time-step Energetic cost of depsoiting protein and fat. Heat for product formation
        /// </summary>
        public double ForProductFormationAverage { get; set; } = 0.0;

        /// <summary>
        /// Method to calulate running ME Average for today and last timestep
        /// </summary>
        public void UpdateProductFormationAverage()
        {
            if (ForProductFormationAverage == 0)
                ForProductFormationAverage = ForProductFormation;
            else
                ForProductFormationAverage = (ForProductFormationAverage + ForProductFormation) / 2.0;
        }

        /// <summary>
        /// Total energetic cost of heat production
        /// </summary>
        public double ForHeatProduction { get { return ForBasalMetabolism + ForHPViscera + ForProductFormationAverage;} } 


        /// <summary>
        /// Energy available after maintenance
        /// </summary>
        public double AfterMaintenance { get { return FromIntake - ForMaintenance - ForProductFormationAverage; } }

        /// <summary>
        /// Energy used for fetal development
        /// </summary>
        public double ForFetus { get; set; }

        /// <summary>
        /// Energy available after accounting for pregnancy
        /// </summary>
        public double AfterPregnancy { get { return AfterMaintenance - ForFetus; } }

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
        public double AfterWool { get { return AfterLactation - ForWool; } }

        /// <summary>
        /// Energy available for growth
        /// </summary>
        public double Net { get { return AfterWool; } }

        /// <summary>
        /// Energy available for growth
        /// </summary>
        public double AvailableForGain { get { return AfterWool; } }

        /// <summary>
        /// Energy for gain after accounting for efficiency
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Energy of protein (non-viscera protein in Oddy)
        /// </summary>
        public RuminantTrackingItem Protein { get; set; }

        /// <summary>
        /// Energy of visceral protein (empty gut, liver, kidneys, heart, and lungs) used in Oddy
        /// </summary>
        public RuminantTrackingItem ProteinViscera { get; set; }

        /// <summary>
        /// Energy used for fat
        /// </summary>
        public RuminantTrackingItem Fat { get; set; }

        /// <summary>
        /// Sum total protein energy accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinTotal { get { return (Protein?.Amount ?? 0) + (ProteinViscera?.Amount ?? 0); } }

        /// <summary>
        /// Sum change in dry protein energy accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinChange { get { return (Protein?.Change ?? 0) + (ProteinViscera?.Change ?? 0); } }

        /// <summary>
        /// Total fat energy accounting for missing Fat pool
        /// </summary>
        public double FatTotal { get { return (Fat?.Amount ?? 0); } }

        /// <summary>
        /// Change in fat energy accounting for missing fat pool
        /// </summary>
        public double FatChange { get { return (Fat?.Change ?? 0); } }

        /// <summary>
        /// Efficiency growth
        /// </summary>
        public double Kg { get; set; }

        /// <summary>
        /// Efficiency maintenance
        /// </summary>
        public double Km { get; set; }

        /// <summary>
        /// Efficiency lactation
        /// </summary>
        public double Kl { get; set; }

        /// <summary>
        /// Reset all running stores
        /// </summary>
        public void Reset()
        {
            ForBasalMetabolism = 0;
            ForProductFormation = 0;
            ForHPViscera = 0;
            ForFetus = 0;
            ForLactation = 0;
            ForWool = 0;
            ToMove = 0;
            ToGraze = 0;
            ForGain = 0;
        }

    }
}
