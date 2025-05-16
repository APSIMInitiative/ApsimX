namespace APSIM.Core;

public interface INodeModel
{
    string Name { get; }
    string ResourceName { get; }
    bool Enabled { get; set; }
    bool IsHidden { get; set; }
    void SetParent(INodeModel parent);

    void OnCreated();
    IEnumerable<INodeModel> GetChildren();
    void AddChild(INodeModel childModel);
    void InsertChild(int index, INodeModel childModel);
    void RemoveChild(INodeModel childModel);
}