namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface to provide specified filename for html output produced
    /// </summary>
    public interface ISpecificOutputFilename
    {
        /// <summary>
        /// Name of output filename
        /// </summary>
        string HtmlOutputFilename { get; }
    }
}
