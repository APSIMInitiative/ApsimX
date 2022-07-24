namespace Models.Core.Replace
{
    using Models.Core.ApsimFile;
    using System;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// Searches all descendents of a relativeTo model looking for a model that
    /// matches a specific type or name. For each match it replacements the 
    /// found model with a replacement.
    /// </summary>
    [Serializable]
    public class ModelReplacement : IReplacement
    {
        /// <summary>The name of the model to find.</summary>
        private string modelNameToFind;

        /// <summary>The type name of the model to find.</summary>
        private string modelTypeToFind;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private IModel replacement;

        /// <summary>Constructor</summary>
        /// <param name="nameToFind">Model name to search for.</param>
        /// <param name="typeToFind">Model type to search for. Can be null to only search by name.</param>
        /// <param name="modelReplacement">The value to Model path to use to find the model to replace.</param>
        public ModelReplacement(string nameToFind, string typeToFind, IModel modelReplacement)
        {
            modelNameToFind = nameToFind;
            modelTypeToFind = typeToFind;
            replacement = modelReplacement;
        }

        /// <summary>Perform the replacement.</summary>
        /// <param name="parent">The parent model to search under for models to replace.</param>
        public void Replace(IModel parent)
        {
            // Try replacement by name match first. If not found then try by type name.
            bool replacementWasMade = DoReplacement(parent, (m) => m.Name.Equals(modelNameToFind, StringComparison.InvariantCultureIgnoreCase));
            if (!replacementWasMade && modelTypeToFind != null)
                DoReplacement(parent, (m) => m.GetType().Name.Equals(modelTypeToFind, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Perform a replacement on all modesl that match a filter.
        /// </summary>
        /// <param name="parent">The parent model to search under.</param>
        /// <param name="filter">The filter to use.</param>
        /// <returns>True if replacement was found.</returns>
        private bool DoReplacement(IModel parent, Func<IModel, bool> filter)
        {
            bool replacementWasMade = false;
            foreach (var match in parent.FindAllDescendants()
                                        .Where(m => filter(m))
                                        .ToArray()) // ToArray is necessary to stop 'Collection was modified' exception
            {
                replacementWasMade = true;
                Structure.Replace(match, replacement);
            }
            return replacementWasMade;
        }

        /// <summary>
        /// Determine value-equality to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        public override bool Equals(object obj)
        {
            if (obj is ModelReplacement model)
                return modelNameToFind == model.modelNameToFind && replacement.Equals(model.replacement);
            return false;
        }

        /// <summary>
        /// Get a hash code for this model replacement instance.
        /// Different instance which are equal in value should return
        /// the same hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return (modelNameToFind, replacement).GetHashCode();
        }
    }
}
