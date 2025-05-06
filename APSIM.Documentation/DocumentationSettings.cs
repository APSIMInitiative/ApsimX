namespace APSIM.Documentation
{

    /// <summary>
    /// Class that holds settings variables for generating documentation. Passed into documentation functions from the Top Level.
    /// </summary>
    public class DocumentationSettings
    {
        /// <summary>
        /// If GenerateGraphs is true, graph models will generate and display graphs in the documentation. By default is off.
        /// </summary>
        public static bool GenerateGraphs {get;set;} = false;
        
    }
}