using APSIM.Core;

namespace Models.Core
{
    /// <summary>An interface for a model that creates dynamic temporary nodes</summary>
    public interface IGenerateNodes
    {
        /// <summary>
        /// 
        /// </summary>
        public void CreateCommands();

        /// <summary>
        /// 
        /// </summary>
        public bool CreateNodes();

        /// <summary>
        /// 
        /// </summary>
        public bool DeleteNodes();

        /// <summary>
        /// DELETE ME LATER
        /// </summary>
        public APSIMFilePath FilePath { get; set; }
    }
}
