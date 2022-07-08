namespace Models.Core.Replace
{
    using Models.Core.ApsimFile;
    using System;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    [Serializable]
    public class ModelReplacement : IReplacement
    {
        /// <summary>
        /// Searches all descendents of a relativeTo model looking for a model that
        /// matches a specific type or name. For each match it replacements the 
        /// found model with a replacement.
        /// </summary>
        private string modelNameOrTypeToFind;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private IModel replacement;

        /// <summary>Constructor</summary>
        /// <param name="nameOrTypeToFind">Model name or type to find the model to replace.</param>
        /// <param name="modelReplacement">The value to Model path to use to find the model to replace.</param>
        public ModelReplacement(string nameOrTypeToFind, IModel modelReplacement)
        {
            modelNameOrTypeToFind = nameOrTypeToFind;
            replacement = modelReplacement;
        }

        /// <summary>Perform the replacement.</summary>
        /// <param name="parent">The parent model to search under for models to replace.</param>
        public void Replace(IModel parent)
        {
            foreach (var match in parent.FindAllDescendants()
                                            .Where(desc => desc.Name.Equals(modelNameOrTypeToFind, StringComparison.InvariantCultureIgnoreCase) ||
                                                           desc.GetType().Name.Equals(modelNameOrTypeToFind, StringComparison.InvariantCultureIgnoreCase))
                                            .ToArray()) // ToArray is necessary to stop 'Collection was modified' exception
                Structure.Replace(match, replacement);
        }
    }
}
