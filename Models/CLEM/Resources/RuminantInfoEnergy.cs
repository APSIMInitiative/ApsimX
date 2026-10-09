using System;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Store of Ruminant energy for the time-step
    /// </summary>
    [Serializable]
    public class RuminantInfoEnergy
    {
        private readonly Ruminant ruminant;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ruminant">Reference to the ruminant</param>
        public RuminantInfoEnergy(Ruminant ruminant)
        {
            this.ruminant = ruminant;
        }

        /// <summary>
        /// The potential and actual MJ milk intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer MilkDaily { get; set; } = new ExpectedActualContainer();

        /// <summary>
        /// Energy obtained from intake (MJ/day)
        /// </summary>
        public double FromIntake { get { return ruminant.Intake.ME; } }

        /// <summary>
        /// Energy used for basal metabolism (MJ/day)
        /// </summary>
        public double ForBasalMetabolism { get; set; }

        /// <summary>
        /// Energy used for HP Viscera (MJ/day)
        /// </summary>
        public double ForHPViscera { get; set; }

        /// <summary>
        /// Energy used to move while grazing/mustering etc (MJ/day)
        /// </summary>
        public double ToMove { get; set; }

        /// <summary>
        /// Energy used to graze while grazing (MJ/day)
        /// </summary>
        public double ToGraze { get; set; }

        /// <summary>
        /// Total Energy used for grazing (ToMove + ToGraze) 
        /// </summary>
        public double ForGrazing { get { return ToMove + ToGraze; } }

        /// <summary>
        /// Energy used for maintenance (MJ/day)
        /// </summary>
        public double ForMaintenance { get { return ForBasalMetabolism + ForHPViscera + ForGrazing; } }

        /// <summary>
        /// Energetic cost of depositing protein and fat. Heat for product formation (MJ/day)
        /// </summary>
        public double ForProductFormation { get; set; } = 0.0;

        /// <summary>
        /// Averaged time-step energetic cost of depositing protein and fat. Heat for product formation (MJ/day)
        /// </summary>
        public double ForProductFormationAverage { get; set; } = 0.0;

        /// <summary>
        /// Method to calculate running ME Average for today and last timestep
        /// </summary>
        public void UpdateProductFormationAverage()
        {
            if (ForProductFormationAverage == 0)
                ForProductFormationAverage = ForProductFormation;
            else
                ForProductFormationAverage = (ForProductFormationAverage + ForProductFormation) / 2.0;
        }

        /// <summary>
        /// Total energetic cost of heat production (MJ/day)
        /// </summary>
        public double ForHeatProduction { get { return ForBasalMetabolism + ForHPViscera + ForProductFormationAverage;} }

        /// <summary>
        /// Energy available after maintenance (MJ/day)
        /// </summary>
        public double AfterMaintenance { get { return FromIntake - ForMaintenance - ForProductFormationAverage; } }

        /// <summary>
        /// Energy used for fetal development (MJ/day)
        /// </summary>
        public double ForFetus { get; set; }

        /// <summary>
        /// Energy available after accounting for pregnancy (MJ/day)
        /// </summary>
        public double AfterPregnancy { get { return AfterMaintenance - ForFetus; } }

        /// <summary>
        /// Energy used for milk production (MJ/day) (E in in milk + cost of making milk)
        /// </summary>
        public double ForLactation { get; set; }

        /// <summary>
        /// Energy used for protein mobilisation (MJ/day)
        /// </summary>
        public double ForProteinMobilisation { get; set; }

        /// <summary>
        /// Energy available after lactation demands (MJ/day)
        /// </summary>
        public double AfterLactation { get { return AfterPregnancy - ForLactation - ForProteinMobilisation; } }

        /// <summary>
        /// Energy used for wool production (MJ/day)
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Energy stored in clean wool
        /// </summary>
        public double StoredInWool { get { return ruminant.Weight.WoolClean.Amount * 24.0; } }

        /// <summary>
        /// Energy available after wool demands
        /// </summary>
        public double AfterWool { get { return AfterLactation - ForWool; } }

        /// <summary>
        /// Energy available for growth (MJ/day)
        /// </summary>
        public double Net { get { return AfterWool; } }

        /// <summary>
        /// Energy available for growth (MJ/day)
        /// </summary>
        public double AvailableForGain { get { return AfterWool; } }

        /// <summary>
        /// Energy for gain after accounting for efficiency
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Energy for gain up to the normalised for age
        /// </summary>
        public double ForDesiredGain { get; set; }

        /// <summary>
        /// Energy of protein (non-viscera protein in Oddy)
        /// </summary>
        public RuminantTrackingItemBodyStore Protein { get; set; }

        /// <summary>
        /// Energy of visceral protein (empty gut, liver, kidneys, heart, and lungs) used in Oddy
        /// </summary>
        public RuminantTrackingItemBodyStore ProteinViscera { get; set; }

        /// <summary>
        /// Energy used for fat
        /// </summary>
        public RuminantTrackingItemBodyStore Fat { get; set; }

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
        /// Efficiency wool
        /// </summary>
        public double Kw { get; set; } = 0.18;

        /// <summary>
        /// Reset all running stores
        /// </summary>
        public void Reset()
        {
            ForBasalMetabolism = 0;
            ForProductFormation = 0;
            ForHPViscera = 0;
            ForLactation = 0;
            ForProteinMobilisation = 0;
            ForWool = 0;
            ToMove = 0;
            ToGraze = 0;
            ForGain = 0;
        }

        /// <summary>
        /// Reset all BodyStore Tracking items for time step
        /// </summary>
        public void TimeStepReset()
        {
            Protein?.TimeStepReset();
            ProteinViscera?.TimeStepReset();
            Fat?.TimeStepReset();
        }

        /// <summary>
        /// A method to return the proportion of energy provided for a pahse of allocation
        /// </summary>
        /// <param name="allocationPhase">The name of the allocation phase</param>
        /// <returns>Proportion of energy provided for the phase or -9999 if phase unknown</returns>
        public double ProportionAvailable(string allocationPhase)
        {
            double needed;
            double after;

            switch (allocationPhase)
            {
                case "Metabolism":
                    needed = ForBasalMetabolism + ForProductFormationAverage;
                    after = FromIntake - ForBasalMetabolism - ForProductFormationAverage;
                    break;
                case "Movement":
                    needed = ForGrazing;
                    after = FromIntake - ForMaintenance;
                    break;
                case "Maintenance":
                    needed = ForMaintenance + ForProductFormationAverage ;
                    after = AfterMaintenance;
                    break;
                case "Pregnancy":
                    needed = ForFetus;
                    after = AfterPregnancy;
                    break;
                case "Lactation":
                    needed = ForLactation;
                    after = AfterPregnancy - ForLactation;
                    break;
                case "ProteinMobilisation":
                    needed = ForProteinMobilisation;
                    after = AfterLactation;
                    break;
                case "Wool":
                    needed = ForWool;
                    after = AfterWool;
                    break;
                case "DesiredGain":
                    needed = ForDesiredGain;
                    after = AfterWool;
                    break;
                default:
                    return -9999;
            }
            if (needed == 0)
                return double.NaN;

            return Math.Max(0.0, needed + Math.Min(after, 0.0)) / needed;
        }
    }
}
