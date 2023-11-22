namespace Models.Utilities
{
    /// <summary>
    /// Collection of units conversion extension functions
    /// Doubles conversion only - no checking if the calc is a legitimate one
    /// </summary>
    public static class UnitsConverter
    {
        const double smm2m = 1e-6;
        /// <summary>Convert Millimetres Squared to Metres Squared </summary>
        public static double ConvertSqM2SqMM(this double val)
        {
            return val * smm2m;
        }
    }
}
