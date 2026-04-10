using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.Factorial
{
    /// <summary>
    /// This class permutates all child models by each other.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.QuadView")]
    [PresenterName("UserInterface.Presenters.QuadPresenter")]
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Description("Generate factors as specified in an Excel file")]
    public class FactorsFromFile: Model, IReferenceExternalFiles, IGenerateNodes, ILineEditor
    {
        private string _filename { get; set; } = null;

        private DataTable _data { get; set; } = null;

        private string[] _commands { get; set; } = new string[0];

        private string[] _createdNodes { get; set; } = new string[0];

        /// <summary>
        /// The name of the Excel spreadsheet containing a sheet for each 
        /// factor to  be created. Sheet names define the factors created.
        /// </summary>
        [Description("Factor details spreadsheet")]
        [Display(Type = DisplayType.FileName)]
        public string FileName { 
            get 
            {
                if (Node != null)
                    return PathUtilities.GetRelativePath(_filename, Node.FileName);
                else
                    return _filename;
            } 
            set
            {
                _filename = value;
            }
        }

        /// <summary>
        /// The column in the data table that is used to label the composite 
        /// factor that is built from that row.
        /// </summary>
        [Description("Include Sheets (Comma seperated)")]
        public string[] Sheets { get; set; }

        /// <summary>
        /// The column in the data table that is used to label the composite 
        /// factor that is built from that row.
        /// </summary>
        [Description("Name Column")]
        public string NameColumn { get; set; }

        /// <summary>
        /// Property to update in order to trigger OnModelChanged events.
        /// </summary>
        [Description(" ")]
        [Display(Type = DisplayType.Button)]
        [JsonIgnore]
        public bool Refresh {get;set;} = false;

        /// <summary>
        /// Gets or sets the full file name (with path). 
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    throw new FileNotFoundException($"Factors spreadsheet must be supplied");
                else
                {
                    Simulations simulations = Node.FindParent<Simulations>(recurse: true);
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(FileName, simulations.FileName);
                    else
                        return FileName;
                }
            }
        }

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return [FullFileName];
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Lines { 
            get {return _commands;} 
            set {return;}
        }

        /// <summary></summary>
        public void RemovePathsFromReferencedFileNames()
        {
            FileName = Path.GetFileName(FileName);
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public bool GenerateNodes()
        {
            _commands = new string[0];

            string relativeDirectory = Path.GetDirectoryName(Node.FileName);
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(NameColumn))
                return false;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
             if (experiment == null)
                return false;
            
            _commands = GetCommands().ToArray();
            if (_commands.Count() > 0)
            {
                IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(_commands, experiment, relativeDirectory);
                CommandProcessor.Run(commands, experiment, runner: null);
            }

            foreach(string path in _createdNodes)
            {
                VariableComposite variable = Node.GetObject(path, LocatorFlags.ModelsOnly, experiment);
                (variable.FirstModel as ICreatable).OnCreated();
                if (variable.FirstModel is IGenerateNodes generator)
                    generator.GenerateNodes();
            }

            return true;
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public bool CleanNodes()
        {
            string relativeDirectory = Path.GetDirectoryName(Node.FileName);
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(NameColumn))
                return false;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment == null)
                return false;

            List<string> commands = new List<string>();
            foreach(string name in _createdNodes)
                commands.Add($"delete {name}");

            if (commands.Count > 0)
                CommandProcessor.Run(CommandLanguage.StringToCommands(commands, experiment, relativeDirectory), experiment, runner: null);

            return true;
        }

        /// <summary>
        /// Method to read factors from an excel spreadsheet and populate parent model with factors and composite factor components.
        /// </summary>
        private string[] GetCommands()
        {
            if (string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(NameColumn))
                return new string[0];

            IModel parent = Node.FindParent<IModel>();
            if (parent == null)
                return ["### FactorFromFile cannot find parent ###"];

            List<string> sheets = new List<string>();
            try
            {
                sheets = FileUtilities.ReadSheetNamesFromExcelFile(FullFileName).ToList();
            }
            catch (Exception exception)
            {
                return ["### Error Reading input file ###", 
                        "### " + exception.Message + " ###"];
            }

            List<string> commands = new List<string>();
            List<string> createdNodes = new List<string>();
            sheets.Reverse(); //do the children in oppersite order
            foreach(string sheet in sheets)
            {
                if (Sheets == null || Sheets.Count() == 0 || Sheets.Contains(sheet))
                {
                    commands.Add($"add new FactorFromFile to [{parent.Name}] name {sheet}");
                    commands.Add($"move [{parent.Name}].{sheet} after [{parent.Name}].{Name}");
                    commands.Add($"[{parent.Name}].{sheet}.FileName = {FileName}");
                    commands.Add($"[{parent.Name}].{sheet}.Sheet = {sheet}");
                    commands.Add($"[{parent.Name}].{sheet}.NameColumn = {NameColumn}");
                    commands.Add($"[{parent.Name}].{sheet}.ReadOnly = true");
                    createdNodes.Add($"[{parent.Name}].{sheet}");
                }
            }
            _createdNodes = createdNodes.ToArray();
            return commands.ToArray();
        }
    }
}
