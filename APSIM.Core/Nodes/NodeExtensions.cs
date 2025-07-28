namespace APSIM.Core;

public static class NodeExtensions
{
    public static Node FindInScope(this Node node, string name) => node.WalkScoped().FirstOrDefault(n => n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    public static IEnumerable<Node> Siblings(this Node node)
    {
        if (node.Parent != null)
            return node.Parent.Children.Where(child => child != node);
        else
            return Enumerable.Empty<Node>();
    }
}