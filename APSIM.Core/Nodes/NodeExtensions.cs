namespace APSIM.Core;

public static class NodeExtensions
{
    /// <summary>Walk nodes in scope (depth first), returing each node</summary>
    public static IEnumerable<T> WalkScopedModels<T>(this Node node)
    {
        return node.WalkScoped().Select(n => (T)n.Model);
    }

    public static IEnumerable<Node> FindAllInScope(this Node node, string name) => node.WalkScoped().Where(n => n.Name == name);

    public static Node FindInScope(this Node node, string name) => node.WalkScoped().FirstOrDefault(n => n.Name == name);

}