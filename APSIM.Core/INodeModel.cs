namespace APSIM.Core;

public interface INodeModel
{
    string Name { get; }
    IEnumerable<object> GetChildren();
    void SetParent(object parent);
}