using System;
using System.Collections.Generic;
using APSIM.Soils;

namespace Models.Core;

/// <summary>
/// Maintains a parent / child relationship for all models.
/// </summary>
public class ModelDiscovery
{
    private Dictionary<Type, ModelNodeTree.DiscoveryFuncDelegate> typeToChildrenMap = new()
    {
        { typeof(Organic), (obj) => ("Organic", null) }
    };




    /// <summary>
    ///
    /// </summary>
    /// <param name="t"></param>
    /// <param name="f"></param>
    public void RegisterType(Type t, ModelNodeTree.DiscoveryFuncDelegate f)
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
        if (obj is IModel model)
            return (model.Name, model.Children);
        else if (typeToChildrenMap.TryGetValue(obj.GetType(), out ModelNodeTree.DiscoveryFuncDelegate func))
            return func(obj);
        else
            throw new Exception($"Unknown node type: {obj.GetType()}");
    }
}