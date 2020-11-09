namespace Models.Interfaces
{
    /// <summary>
    /// This interface describes a soil temperature model.
    /// </summary>
    public interface ISoilTemperature
    {
        /// <summary>Returns soil temperature for each layer (oc)</summary>
        double[] Value { get; }
    }
}
