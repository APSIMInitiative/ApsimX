using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Models.Core;
using APSIM.Core;
using System.IO;
using Models.Storage;
using Models.Climate;
using APSIM.Shared.Utilities;
using Models.PostSimulationTools;

namespace Models.PreSimulationTools
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.QuadView")]
    [PresenterName("UserInterface.Presenters.QuadPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Import : Model, IReferenceExternalFiles, ICodeEditor, IGenerateNodes
    {
        /// <summary></summary>
        internal string _filename = "";

        /// <summary>The list of commands that are generated</summary>
        private string[] _commands { get; set; } = [];

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public APSIMFilePath FilePath { get; set; } = new APSIMFilePath();

        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        [Description("APSIM file")]
        [Display(Type = DisplayType.FileName)]
        public string FileName
        {
            get { return FilePath.RelativeFilePath; }
            set { FilePath.RelativeFilePath = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Description("Path to the model in other APSIM file to copy from")]
        public string ModelPath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Code
        { 
            get { return _commands; } 
            set { return; }
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
        /// 
        /// </summary>
        public void CreateCommands()
        {
            _commands = GetCommands(this, FileName, ModelPath);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CreateNodes()
        {
            _commands = [];

            string relativeDirectory = FilePath.StartDirectory;
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(ModelPath))
                return false;

            bool readOnly = ReadOnly;
            try
            {
                //Check if this model is read only, and disable temporarily while generating the children
                if (readOnly)
                    ReadOnly = false;

                //Create the commands list
                CreateCommands();
                
                //Lines will pull from _generatedCommads as a 1D array
                IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(_commands, Parent.Node.Model, relativeDirectory);
                CommandProcessor.Run(commands, Parent.Node.Model, runner: null);
            }
            catch (Exception exception)
            {
                _commands = [];
                throw new Exception("Import failed to import from the input file.", exception);
            }
            finally //reset the read only status
            {
                ReadOnly = readOnly;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DeleteNodes()
        {
            List<IModel> childrenToDelete = new List<IModel>();
            foreach(IModel child in Children)
                if (child.ReadOnly == true)
                    childrenToDelete.Add(child);
            foreach(IModel child in childrenToDelete)
                Node.RemoveChild(child.Node.Model);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        private static string[] GetCommands(IModel import, string fileName, string path)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(path))
                return [];

            bool getChildren = false;
            if (path.ToLower() == "simulations" || path.ToLower() == "[simulations]")
                getChildren = true;

            List<string> commands = new List<string>();
            if (!getChildren)
            {
                commands.Add($"add {path} from {fileName} to [{import.Name}]");
                commands.Add($"[{import.Name}].{path}.ReadOnly = True");
            }
            else
            {
                string localDirectory = Path.GetDirectoryName(import.Node.FileName);
                string referenceDirectory = Path.GetDirectoryName(fileName);
                Simulations otherFile = FileFormat.ReadFromFile<Simulations>(fileName).Model as Simulations;
                foreach (IModel child in otherFile.Children)
                {
                    commands.Add($"add [Simulations].{child.Name} from {fileName} to [{import.Name}]");
                    commands.Add($"[{import.Name}].{child.Name}.ReadOnly = True");
                    foreach(Node node in child.Node.Walk())
                    {
                        if (node.Model is Weather weather)
                        {
                            string absolutePath = PathUtilities.GetAbsolutePath(weather.FileName, referenceDirectory);
                            string relativePath = PathUtilities.GetRelativePath(absolutePath, localDirectory);
                            string modelPath = weather.FullPath.Replace($".Simulations.", $"[{import.Name}].");
                            commands.Add($"{modelPath}.FileName = {relativePath}");
                        }
                        else if (node.Model is ExcelInput excelInput)
                        {
                            List<string> newFileNames = new List<string>();
                            foreach(string filename in excelInput.FileNames)
                            {
                                string absolutePath = PathUtilities.GetAbsolutePath(filename, referenceDirectory);
                                string relativePath = PathUtilities.GetRelativePath(absolutePath, localDirectory);
                                newFileNames.Add(relativePath);
                            }

                            string modelPath = excelInput.FullPath.Replace($".Simulations.", $"[{import.Name}].");
                            commands.Add($"{modelPath}.FileNames = {string.Join(',', newFileNames)}");
                        }
                    }
                }
            }
            return commands.ToArray();
        }

        /// <summary> Called when the supplement is created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();
            FilePath.SetStartDirectory(Path.GetDirectoryName(Node.FileName));
        }
    }
}
