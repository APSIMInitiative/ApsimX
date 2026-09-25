namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Conception status
    /// </summary>
    public enum ConceptionStatus
    {
        /// <summary>
        /// Female just conceived
        /// </summary>
        Conceived,
        /// <summary>
        /// Prenatal or at birth mortality
        /// </summary>
        Failed,
        /// <summary>
        /// Successful birth
        /// </summary>
        Birth,
        /// <summary>
        /// Survived to wean
        /// </summary>
        Weaned,
        /// <summary>
        /// Unsuccessful mating
        /// </summary>
        Unsuccessful,
        /// <summary>
        /// Not mated
        /// </summary>
        NotMated,
        /// <summary>
        /// Not ready
        /// </summary>
        NotReady,
        /// <summary>
        /// Not available
        /// </summary>
        NotAvailable,
        /// <summary>
        /// No breeding has happened
        /// </summary>
        NoBreeding
    }
}
