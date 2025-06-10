namespace APSIM.Core;

/// <summary>
/// Implements APSIM's scoping rules.
/// </summary>
internal class ScopingRules
{
    private Dictionary<Node, List<Node>> cache = new();

    /// <summary>
    /// Return a list of models in scope to the one specified.
    /// </summary>
    /// <param name="relativeTo">The model to base scoping rules on</param>
    public IEnumerable<Node> FindAll(Node relativeTo)
    {
        Node scopedParent = FindScopedParentModel(relativeTo);
        if (scopedParent == null)
            throw new Exception("No scoping model found relative to: " + relativeTo.FullNameAndPath);

        // Try the cache first.
        if (cache.TryGetValue(scopedParent, out List<Node> modelsInScope))
            return modelsInScope;

        // The algorithm is to find the parent scoped model of the specified model.
        // Then return all descendants of the scoped model and then recursively
        // the direct children of the parents of the scoped model. For any direct
        // child of the parents of the scoped model, we also return its descendants
        // if it is not a scoped model.

        // Return all models in zone and all direct children of zones parent.
        modelsInScope = new List<Node>();
        modelsInScope.AddRange(scopedParent.Walk());
        Node m = scopedParent;
        while (m.Parent != null)
        {
            //m = m.Parent;
            modelsInScope.Add(m.Parent);
            foreach (Node child in m.Parent.Children)
            {
                if (child != m)
                {
                    modelsInScope.Add(child);

                    // Return the child's descendants if it is not a scoped model.
                    // This ensures that a soil's water node will be in scope of
                    // a manager inside a folder inside a zone.
                    if (child.Model is not IScopedModel)
                        modelsInScope.AddRange(child.Walk().Skip(1));
                }
            }
            m = m.Parent;
        }

        if (!modelsInScope.Contains(m))
            modelsInScope.Add(m); // top level simulation

        // Scope doesn't work when a manager is under a factor and the manager refers to a model that is in the experiment base simulation.
        // In this case we need to add all models that are in the base simulation to the modelInScope.
        var exp = relativeTo.WalkParents().FirstOrDefault(p => p.Model.GetType().Name == "Experiment");
        if (exp != null)
        {
            var sim = exp.Children.FirstOrDefault(c => c.Model.GetType().Name == "Simulation");

            var descendants = sim.Walk().Skip(1);
            modelsInScope.AddRange(descendants);
        }

        // add to cache for next time.
        cache.Add(scopedParent, modelsInScope);
        return modelsInScope;
    }

    /// <summary>
    /// Find a parent of 'relativeTo' that has a [ScopedModel] attribute.
    /// Returns null if non found.
    /// </summary>
    /// <param name="relativeTo">The model to use as a base.</param>
    public Node FindScopedParentModel(Node relativeTo)
    {
        if (relativeTo.Model is IScopedModel || relativeTo.Parent == null)
            return relativeTo;
        return relativeTo.WalkParents().First(p => p.Model is IScopedModel || p.Parent == null);
    }

    /// <summary>
    /// Returns true if model x is in scope of model y.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public bool InScopeOf(Node x, Node y)
    {
        return FindAll(y).Contains(x);
    }

    /// <summary>
    /// Clear the current cache
    /// </summary>
    public void Clear()
    {
        cache.Clear();
    }
}
