namespace Models.DCAPST
{
    /// <summary>
    /// A generic crop parameter generator interface.
    /// </summary>
    public interface ICropParameterGenerator
    {
        /// <summary>
        /// Given the supplied crop name, return the specific crop DCaPST parameters.
        /// </summary>
        /// <param name="cropName"></param>
        /// <returns>The DCaPSTParameters relevant to the specified crop name.</returns>
        public DCaPSTParameters Generate(string cropName);

        /// <summary>
        /// Allows us to lift the AC curve.
        /// </summary>
        /// <param name="cropName"></param>
        /// <param name="dcapstParameters"></param>
        /// <param name="rubiscoLimitedModifier"></param>
        public void ApplyRubiscoLimitedModifier(string cropName, DCaPSTParameters dcapstParameters, double rubiscoLimitedModifier);

        /// <summary>
        /// Allows us to lift the AJ curve.
        /// </summary>
        /// <param name="cropName"></param>
        /// <param name="dcapstParameters"></param>
        /// <param name="electronTransportLimitedModifier"></param>
        public void ApplyElectronTransportLimitedModifier(string cropName, DCaPSTParameters dcapstParameters, double electronTransportLimitedModifier);
    }
}