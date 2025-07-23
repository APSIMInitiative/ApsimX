namespace APSIM.Core;

public static class NodeExtensions
{
    /// <summary>Walk nodes in scope (depth first), returing each node</summary>
    public static IEnumerable<T> WalkScopedModels<T>(this Node node)
    {
        return node.WalkScoped().Select(n => (T)n.Model);
    }

    public static IEnumerable<Node> FindAllInScope(this Node node, string name) => node.WalkScoped().Where(n => n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    public static Node FindInScope(this Node node, string name) => node.WalkScoped().FirstOrDefault(n => n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    public static IEnumerable<Node> Siblings(this Node node)
    {
        if (node.Parent != null)
            return node.Parent.Children.Where(child => child != node);
        else
            return Enumerable.Empty<Node>();
    }
}