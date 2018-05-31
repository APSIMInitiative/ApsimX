namespace UserInterface.Intellisense
{
    /// <summary>
    /// This interface allows to provide more information for scripts such as using statements, etc.
    /// </summary>
    public interface ICSharpScriptProvider
    {
        /// <summary>
        /// Gets the using statemtns in the script.
        /// </summary>
        /// <returns>The using statemtns in the script.</returns>
        string GetUsing();

        /// <summary>
        /// Gets the variables in the script.
        /// </summary>
        /// <returns>The variables in the script.</returns>
        string GetVars();

        /// <summary>
        /// Gets the namespace that the script resides in.
        /// </summary>
        /// <returns>The namespace that the script resides in.</returns>
        string GetNamespace();
    }
}
