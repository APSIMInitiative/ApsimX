using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PreSimulationTools
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.QuadView")]
    [PresenterName("UserInterface.Presenters.QuadPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    public class Import : Model, IPreSimulationTool, IReferenceExternalFiles, IGenerateNodes, ICodeEditor
    {
        /// <summary></summary>
        internal string _filename;

        /// <summary></summary>
        internal string _modelName;

        /// <summary>The list of commands that are generated</summary>
        private string[] _generatedCommands { get; set; } = null;

        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        [Description("APSIM file")]
        [Display(Type = DisplayType.FileName)]
        public string FileName
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Description("Path to the model in other APSIM file to copy from")]
        public string ModelName
        {
            get
            {
                return _modelName;
            }
            set
            {
                _modelName = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Code
        { 
            get {
                return _generatedCommands;
            } 
            set {return;}
        }

        /// <summary>
        /// Return our input filenames
        /// </summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return [];
        }

        /// <summary>
        /// Remove all paths from referenced filenames.
        /// </summary>
        public void RemovePathsFromReferencedFileNames()
        {
            return;
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
        {
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool GenerateNodes()
        {
            return true;
        }
    }
}
