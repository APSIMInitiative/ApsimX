namespace Models.Core.Run
{
    using System;

    /// <summary>
    /// This class encapsulates an instruction to replace a property value.
    /// </summary>
    public class PropertyReplacement : IReplacement
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
            if (path == null)
                throw new Exception("No path specified for property replacement.");

            IVariable variable = simulation.FindByPath(path);
            if (variable == null)
                throw new Exception($"Unable to apply property replacement: Unable to resolve path '{path}'.");
            variable.Value = replacement;

            // In a multi-paddock context, we want to attempt to
            // change the property value in all paddocks.
            foreach (Zone paddock in simulation.FindAllDescendants<Zone>())
            {
                variable = paddock.FindByPath(path);
                if (variable != null)
                    variable.Value = replacement;
            }
        }
    }
}
