using Models.CLEM.Resources;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// This <c>SpecifiedRuminantListItem</c> class represents an list of individuals with a cummulative probability of
    /// being selected by random
    /// </summary>
    public class SpecifiedRuminantListItem
    {
        /// <summary>
        /// List index
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Ruminant example
        /// </summary>
        public Ruminant ExampleRuminant { get; set; }
        /// <summary>
        /// The specify ruminant component 
        /// </summary>
        public SpecifyRuminant SpecifyRuminantComponent { get; set; }
        /// <summary>
        /// Cumulative probability of this individual is selected
        /// </summary>
        public double CumulativeProbability { get; set; }
    }
}
