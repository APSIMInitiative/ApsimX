namespace Models.Core.Replace
{
    using System;

    /// <summary>
    /// This class encapsulates an instruction to replace a property value.
    /// </summary>
    [Serializable]
    public class PropertyReplacement
    {
        /// <summary>
        /// Model path to use to find the model to replace. If null, then
        /// multiple replacements are made using the model name for matching.
        /// </summary>
        private string path;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private object replacement;

        /// <summary>Constructor</summary>
        /// <param name="pathOfModel">Model path to use to find the model to replace. If null, then multiple replacements are made using the model name for matching.</param>
        /// <param name="propertyValueReplacement">The value to Model path to use to find the model to replace.</param>
        public PropertyReplacement(string pathOfModel, object propertyValueReplacement)
        {
            path = pathOfModel;
            replacement = propertyValueReplacement;
        }

        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        public void Replace(IModel simulation)
        {
            Overrides.Apply(simulation, new (string, object)[] { (path, replacement) });
        }


        /// <summary>
        /// Check value-equality with another property replacement instance.
        /// </summary>
        /// <remarks>Needed by ManagedSocketconnectionTests when comparing two RunCommands.</remarks>
        /// <param name="obj">The second object instance.</param>
        public override bool Equals(object obj)
        {
            if (obj is PropertyReplacement property)
            {
                if (path != property.path)
                    return false;
                if (replacement == null && property.replacement == null)
                    return true;
                if (replacement == null || property.replacement == null)
                    return false;
                return replacement.Equals(property.replacement);
            }
            return false;
        }

        /// <summary>
        /// Get a hash code for this property replacement instance.
        /// </summary>
        public override int GetHashCode()
        {
            return (path, replacement).GetHashCode();
        }
    }
}
