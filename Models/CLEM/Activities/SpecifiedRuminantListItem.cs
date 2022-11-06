using Models.CLEM.Resources;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// An individual purchase details provided by list of SpecifyRuminant components 
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
        /// Cummulative probability of this one being picked
        /// </summary>
        public double CummulativeProbability { get; set; }
    }
}
