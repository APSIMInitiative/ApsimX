using System.Collections.Generic;
namespace Model.Core
{
    public delegate void ModelAddedDelegate(string NodePath);
    public delegate void PathDelegate(string Path);
    public delegate void NullTypeDelegate();

    /// <summary>
    /// Represents a single point containing a collection of child models.
    /// </summary>
    public interface IZone
    {
        /// <summary>
        /// Name of zone.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Area of the zone.
        /// </summary>
        double Area { get; set; }

        /// <summary>
        /// A list of child models.
        /// </summary>
        List<object> Models { get; set; }

        /// <summary>
        /// Return a full path to this system. Doesn't include the 'Simulations' node.
        /// Format: SimulationName.PaddockName.ChildName
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Find and return a model of the specified type that is in scope.
        /// </summary>
        /// <returns>Returns null if none found.</returns>
        object Find(System.Type ModelType);

        /// <summary>
        /// Find and return a model with the specified name that is in scope.
        /// </summary>
        /// <returns>Returns null if none found.</returns>
        object Find(string ModelName);

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
        /// </summary>
        object Get(string NamePath);

    }
}
