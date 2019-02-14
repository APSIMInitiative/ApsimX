namespace Models.Interfaces
{
    /// <summary>This interface defines what a solute can do.</summary>
    public interface ISolute
    {
        /// <summary>Name of solute.</summary>
        string Name { get;  }

        /// <summary>Solute amount (kg/ha)</summary>
        double[] kgha { get; set; }

        /// <summary>Solute amount (ppm)</summary>
        double[] ppm { get; }

        /// <summary>Setter for kgha.</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        void SetKgHa(SoluteManager.SoluteSetterType callingModelType, double[] value);
    }
}