namespace APSIM.Core;

public interface INodeModel
{
    string Name { get; }
    string ResourceName { get; }
    bool Enabled { get; set; }
    bool IsHidden { get; set; }
    string FullPath { get; }
    Node Node { get; set; }
    void SetParent(INodeModel parent);
    void Rename(string name);

    IEnumerable<INodeModel> GetChildren();
    void AddChild(INodeModel childModel);
    void InsertChild(int index, INodeModel childModel);
    void RemoveChild(INodeModel childModel);
}