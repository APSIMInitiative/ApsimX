namespace Models.Core.ConfigFile
{
    /// <summary>
    /// A config file instruction.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// A insturction keyword describing the instructions action.
        /// </summary>
        public Keyword keyword { get; set; }

        /// <summary>
        /// An optional node that will be modified.
        /// </summary> 
        /// <remarks>
        /// May not be needed in the future with the use of Locator.
        /// </remarks>
        public string NodeToModify { get; set; }

        /// <summary>
        /// An optional file path where a node to copy or add is located.
        /// </summary>
        public string FileContainingNode { get; set; }

        /// <summary>
        /// An optional path to file to save to.
        /// </summary>
        public string SavePath { get; set; }

        /// <summary>
        /// An optional path to file to load from.
        /// </summary>
        public string LoadPath { get; set; }

        /// <summary>
        /// A string representing a node to either add, delete or copy.
        /// </summary>
        /// <remarks>
        /// May not be needed in the future with the use of Locator.
        /// </remarks>
        public string NodeForAction { get; set; }

        /// <summary>
        /// Creates a new Instruction instance with all arguments.
        /// </summary>
        /// 
        public Instruction(Keyword keyword, string nodeToModify, string fileContainingNode, string SavePath, string LoadPath, string nodeForAction)
        {
            this.keyword = keyword;
            this.NodeToModify = nodeToModify;
            this.FileContainingNode = fileContainingNode;
            this.NodeForAction = nodeForAction;
            this.SavePath = SavePath;
            this.LoadPath = LoadPath;
        }

    }

    /// <summary>
    /// Supported keyword types for instruction types.
    /// </summary>
    public enum Keyword
    {
        /// <summary>
        /// Unassigned keyword type.
        /// </summary>
        None,
        /// <summary>
        /// Makes instruction an Add type.
        /// </summary>
        Add,
        /// <summary>
        /// Makes instruction a Copy type.
        /// </summary>
        Copy,
        /// <summary>
        /// Makes instruction a Delete type.
        /// </summary>
        Delete,
        /// <summary>
        /// Makes instruction a Save type.
        /// </summary>
        Save,
        /// <summary>
        /// Makes instruction a Load type.
        /// </summary>
        Load,
        /// <summary>
        /// Makes instruction a Run type.
        /// </summary>
        Run,
        /// <summary>
        /// Makes instruction a duplicate type.
        /// </summary>
        Duplicate

    }
}


