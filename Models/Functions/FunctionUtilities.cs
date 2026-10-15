using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Utilities class for IFunctions
    /// </summary>
    public class FunctionUtilities
    {
        /// <summary>
        /// Static function for checking that the only children of an IFunction 
        /// are functions or memos themselves. This prevents a replacements 
        /// accidently replacing an IFunction with something that won't be 
        /// computed, and hiding a silent failure.
        /// </summary>
        /// <param name="node">Node of Model to check</param>
        public static void ValidateFunctionChildren(Node node)
        {
            IModel model = node.Model as IModel;
            foreach (Node n in node.Children)
                if (n.Model.Enabled && !(n.Model is IFunction || n.Model is IText))
                    throw new System.Exception($"{model.FullPath}: {model.GetType().Name} cannot have children that are not a Function or memo/documentation. A child of {n.Model.GetType().Name} was found.");
        }
    }
}