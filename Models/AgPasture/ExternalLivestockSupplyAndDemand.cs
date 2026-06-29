namespace Models.AgPasture
{
    /// <summary>
    /// Manages any amounts taken or provided from external livestock models to drive the forage removal and
    /// depositing of dung and urine deposits
    /// </summary>
    public class ExternalLivestockSupplyAndDemand
    {
        private double[] speciesProportions;

        /// <summary>
        /// Basic Constructor
        /// </summary>
        public ExternalLivestockSupplyAndDemand()
        {
            Clear();
        }

        /// <summary>
        /// Specify daily urine and dung details
        /// </summary>
        /// <param name="urineN">Urine nitrogen (kg/ha)</param>
        /// <param name="dungN">Dung nitrogen (kg/ha)</param>
        /// <param name="dungDM">Dung dry mass (kg/ha)</param>
        /// <param name="numberOfUrinations">Number of urinations</param>
        public void SetDungAndUrine(double urineN, double dungN, double dungDM, int numberOfUrinations)
        {
            UrineNitrogen = urineN;
            DungNitrogen = dungN;
            DungMass = dungDM;
            NumberOfUrinations = numberOfUrinations;
        }

        /// <summary>
        /// Specify daily 
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="speciesProportions"></param>
        public void SetOfftake(double amount, double[] speciesProportions = null)
        {
            BiomassRequested = amount;
            if (speciesProportions != null)
            {
                // forced setting of species proportions
                this.speciesProportions = speciesProportions;
            }
        }

        /// <summary>
        /// Determines whether pasture was requested by external livestock
        /// </summary>
        public bool PastureRequested => BiomassRequested > 0;

        /// <summary>
        /// Determines whether urine or dung were deposited
        /// </summary>
        public bool UrineDungDeposited => UrineNitrogen + DungNitrogen > 0;

        /// <summary>
        /// The biomass required for grazing (kg/ha)
        /// </summary>
        public double BiomassRequested { get; set; }

        /// <summary>
        /// The amount of nitrogen provided in urine (kg/ha)
        /// </summary>
        public double UrineNitrogen { get; set; }

        /// <summary>
        /// Number of urinations
        /// </summary>
        public int NumberOfUrinations { get; set; }

        /// <summary>
        /// The amount of nitrogen provided in dung (kg/ha)
        /// </summary>
        public double DungNitrogen { get; set; }

        /// <summary>
        /// The dry mass of dung provided (kg/ha)
        /// </summary>
        public double DungMass { get; set; }

        /// <summary>
        /// The proportion of each forages to take
        /// </summary>
        public double[] SpeciesProportionToTake
        {
            // cannot overwrite value already provided by the user.
            get
            {
                return speciesProportions;

            }
            set
            {
                if (speciesProportions is null)
                {
                    speciesProportions = value;
                }
            }
        }

        /// <summary>
        /// Clear the stores to represent no grazing performed.
        /// </summary>
        public void Clear()
        {
            UrineNitrogen = 0;
            DungNitrogen = 0;
            DungMass = 0;
            NumberOfUrinations = 0;
            BiomassRequested = 0;
        }
    }
}
