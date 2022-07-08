namespace Models.Core.Replace
{
    using Models.Core.ApsimFile;
    using System;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    public class ModelReplacementFromResource
    {
        /// <summary>Replace all models with a ResourceName with a new model loaded from a resource.</summary>
        /// <param name="parent">The parent model to search under for models to replace.</param>
        public static void Replace(IModel parent)
        {
            //foreach (var match in parent.FindAllDescendants()
            //                            .Where(m => !string.IsNullOrEmpty(m.ResourceName))
            //                            .ToArray()) // ToArray is necessary to stop 'Collection was modified' exception
            //{
            //    if (replace)
            //    Structure.Replace(match, replacement);
            //}
        }
    }
}
