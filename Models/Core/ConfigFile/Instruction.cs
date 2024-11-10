using System.Collections.Generic;

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
        public Keyword Keyword { get; set; }

        /// <summary>
        /// A string representing a node to either add, delete or copy.
        /// </summary>
        public string ActiveNode { get; set; }

        /// <summary>
        /// An optional node that will be modified.
        /// </summary> 
        public string NewNode { get; set; }

        /// <summary>
        /// An optional path to file to save to.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> Parameters { get; set; }

        /// <summary>
        /// Creates a new Instruction instance with all arguments.
        /// </summary>
        /// 
        public Instruction(Keyword keyword, string activeNode, string newNode, string path, List<string> parameters)
        {
            this.Keyword = keyword;
            this.ActiveNode = activeNode;
            this.NewNode = newNode;
            this.Path = path;
            this.Parameters = parameters;
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


