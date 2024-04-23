namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for all resource attributes
    /// </summary>
    public interface IIndividualAttribute
    {
        /// <summary>
        /// The value of the attribute
        /// </summary>
        object StoredValue { get; set; }

        /// <summary>
        /// The value of the attribute of the most recent mate
        /// </summary>
        object StoredMateValue { get; set; }

        /// <summary>
        /// The style for inheritance of attribute
        /// </summary>
        AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// The settings associated with this attribute
        /// </summary>
        ISetAttribute SetAttributeSettings { get; set; }

        /// <summary>
        /// Creates an attribute of parent type and returns for new offspring
        /// </summary>
        /// <returns>A new attribute inherited from parents</returns>
        object GetInheritedAttribute();
    }
}
