namespace Models.PreSimulationTools.ObservedInfo
{
    /// <summary>
    /// Stores information about a column in an observed table
    /// </summary>
    public class ColumnInfo
    {
        /// <summary></summary>
        public string Name;

        /// <summary></summary>
        public string IsApsimVariable;

        /// <summary></summary>
        public string DataType;

        /// <summary></summary>
        public bool HasErrorColumn;

        /// <summary></summary>
        public string File;
    }
}
