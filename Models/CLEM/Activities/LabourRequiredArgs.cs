namespace Models.CLEM.Activities
{
    /// <summary>
    /// Structure to return values form a labour days request
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="daysNeeded">Number of days required</param>
    /// <param name="category">Category of labour</param>
    /// <param name="relatesToResource">Resource the labour relates to</param>
    public class LabourRequiredArgs(double daysNeeded, string category, string relatesToResource)
    {

        /// <summary>
        /// Calculated days needed
        /// </summary>
        public double DaysNeeded { get; set; } = daysNeeded;

        /// <summary>
        /// Transaction category
        /// </summary>
        public string Category { get; set; } = category;

        /// <summary>
        /// Transaction relates to resource
        /// </summary>
        public string RelatesToResource { get; set; } = relatesToResource;
    }


}
