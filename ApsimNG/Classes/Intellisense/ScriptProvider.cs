namespace UserInterface.Intellisense
{
    /// <summary>
    /// Gets using statements, variables, and namespaces used in a script. 
    /// This class is currently not really needed, (and does not do anything),
    /// but one day it may be useful.
    /// </summary>
    public class ScriptProvider : ICSharpScriptProvider
    {
        /// <summary>
        /// Gets the using statements in the script.
        /// </summary>
        /// <returns>The using statemtns in the script.</returns>
        public string GetUsing()
        {
            return "" +
                "using System; " +
                "using System.Collections.Generic; " +
                "using System.Linq; " +
                "using System.Text; " +
                "using Models; ";
        }

        /// <summary>
        /// Gets the variables in the script.
        /// </summary>
        /// <returns>The variables in the script.</returns>
        public string GetVars()
        {
            return "int age = 25;";
        }

        /// <summary>
        /// Gets the namespace that the script resides in.
        /// </summary>
        /// <returns>The namespace that the script resides in.</returns>
        public string GetNamespace() => null;
    }
}
