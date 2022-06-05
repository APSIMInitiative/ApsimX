namespace Models.CLEM.Activities
{
    /// <summary>
    /// Structure to return values form a labour days request
    /// </summary>
    public class LabourRequiredArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="daysNeeded"></param>
        /// <param name="category"></param>
        /// <param name="relatesToResource"></param>
        public LabourRequiredArgs(double daysNeeded, string category, string relatesToResource)
        {
            DaysNeeded = daysNeeded;
            Category = category;
            RelatesToResource = relatesToResource;
        }

        /// <summary>
        /// Calculated days needed
        /// </summary>
        public double DaysNeeded { get; set; }

        /// <summary>
        /// Transaction category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Transacation relates to resource
        /// </summary>
        public string RelatesToResource { get; set; }
    }


}
