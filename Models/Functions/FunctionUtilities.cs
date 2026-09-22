using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// 
    /// </summary>
    public class FunctionUtilities
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public static void ValidateFunctionChildren(Node node)
        {
            IModel model = node.Model as IModel;
            foreach (Node n in node.Children)
                if (!(n.Model is IFunction || n.Model is IText))
                    throw new System.Exception($"{model.FullPath}: {model.GetType().Name} cannot have children that are not a Function or memo/documentation. A child of {node.Model.GetType().Name} was found.");
        }
    }
}