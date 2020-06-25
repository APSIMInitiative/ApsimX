namespace Models.GrazPlan
{
    /// <summary>
    /// The age information
    /// </summary>
    internal class AgeInfo
    {
        /// <summary>
        /// Proportion
        /// </summary>
        public double Propn;

        /// <summary>
        /// Proportion pregnant
        /// </summary>
        public double[] PropnPreg = new double[4];

        /// <summary>
        /// Proportion lactating
        /// </summary>
        public double[] PropnLact = new double[4];

        /// <summary>
        /// The animal numbers preg and lactating
        /// </summary>
        public int[,] Numbers = new int[4, 4];

        /// <summary>
        /// Gets or sets the age of animal
        /// </summary>
        public int AgeDays { get; set; }

        /// <summary>
        /// Gets or sets the normal base weight
        /// </summary>
        public double NormalBaseWt { get; set; }

        /// <summary>
        /// Gets or sets the animals base weight
        /// </summary>
        public double BaseWeight { get; set; }

        /// <summary>
        /// Gets or sets the fleece weight in kg
        /// </summary>
        public double FleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the age at mating in days
        /// </summary>
        public int AgeAtMating { get; set; }

        /// <summary>
        /// Gets or sets the size at mating in kg
        /// </summary>
        public double SizeAtMating { get; set; }
    }
}
