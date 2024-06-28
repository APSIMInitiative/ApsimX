namespace Models.PMF.Struct
{
    /// <summary>
    /// An enum to describe the tillering method.
    /// </summary>
    public enum TilleringMethodEnum
    {
        /// <summary>
        /// Uses location to determine the fertil tiller number (Australia only).
        /// </summary>
        RulOfThumb = -1,

        /// <summary>
        /// The fertile tiller number is provided by the user.
        /// </summary>
        FixedTillering,

        /// <summary>
        /// The fertile tiller number is calculated dynamically.
        /// </summary>
        DynamicTillering
    }
}
