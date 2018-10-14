namespace Models.Core.Interfaces
{
    /// <summary>
    /// Any class that implements this interface will not have its child models serialised to json.
    /// </summary>
    public interface IDontSerialiseChildren
    {
    }
}
