namespace Models.DCAPST
{
    public interface ICropParameterGenerator
    {
        /// <summary>
        /// Given the supplied crop name, return the specific crop DCaPST parameters.
        /// </summary>
        /// <param name="cropName"></param>
        /// <returns>The DCaPSTParameters relevant to the specified crop name.</returns>
        public DCaPSTParameters Generate(string cropName);
    }
}