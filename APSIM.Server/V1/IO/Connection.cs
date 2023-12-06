namespace APSIM.Server.IO
{
    /// <summary>
    /// These are the supported connection types.
    /// </summary>
    public enum Protocol
    {
        /// <summary>Connection with a native client.</param>
        Native,

        /// <summary>Connection with a managed client.</param>
        Managed
    }
}