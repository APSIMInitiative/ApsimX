namespace APSIM.Core;

public static class NodeExtensions
{
    /// <summary>Walk nodes in scope (depth first), returing each node</summary>
    public static IEnumerable<T> WalkScopedModels<T>(this Node node)
    {
        return node.WalkScoped().Select(n => (T) n.Model);
    }

}