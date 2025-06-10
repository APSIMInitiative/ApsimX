namespace APSIM.Core;

public interface ICreatable
{
    /// <summary>
    /// Called when the model has been newly created in memory, whether from
    /// cloning or deserialisation.
    /// </summary>
    public void OnCreated(Node node);

    /// <summary>
    /// Called when the model is about to be deserialised.
    /// </summary>
    void OnDeserialising();
}
