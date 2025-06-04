namespace APSIM.Core;

public interface ICreatable
{
    /// <summary>
    /// Called when the model has been newly created in memory, whether from
    /// cloning or deserialisation.
    /// </summary>
    public void OnCreated(Node node);
}
