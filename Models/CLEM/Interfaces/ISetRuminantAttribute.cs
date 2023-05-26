namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for all attribute models
    /// </summary>
    public interface ISetAttribute
    {
        /// <summary>
        /// Get an instance of the attribute 
        /// </summary>
        /// <param name="createNewInstance">Recalculate all randomly assigned values</param>
        Resources.IndividualAttribute GetAttribute(bool createNewInstance = true);

        /// <summary>
        /// Name to apply to the attribute
        /// </summary>
        string AttributeName { get; set; }

        /// <summary>
        /// Mandatory attribute
        /// </summary>
        bool Mandatory { get; set; }

    }
}
