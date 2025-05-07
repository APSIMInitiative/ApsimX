namespace APSIM.Core;

/// <summary>
/// Maintains a registry of how to functions that can return information (e.g. name and children) of a POCO.
/// </summary>
public class ModelDiscovery
{
    private Dictionary<Type, NodeTree.DiscoveryFuncDelegate> typeToChildrenMap = new()
    {

    };

    /// <summary>
    ///
    /// </summary>
    /// <param name="t"></param>
    /// <param name="f"></param>
    public void RegisterType(Type t, NodeTree.DiscoveryFuncDelegate f)
    {
        typeToChildrenMap.Add(t, f);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public (string name, IEnumerable<object> children) GetNameAndChildrenOfObj(object obj)
    {
        if (obj is INodeModel model)
            return (model.Name, model.GetChildren());
        else if (typeToChildrenMap.TryGetValue(obj.GetType(), out NodeTree.DiscoveryFuncDelegate func))
            return func(obj);
        else
            throw new Exception($"Unknown node type: {obj.GetType()}");
    }
}