namespace Models.Core.Interfaces
{
    /// <summary>
    /// Any class that implements this interface can optionally have its children serialised.
    /// </summary>
    public interface IOptionallySerialiseChildren
    {
        /// <summary>Allow children to be serialised?</summary>
        bool DoSerialiseChildren { get; }
    }
}
