using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PreSimulationTools.ObservationsInfo;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
    [Description("Generate factors as specified in an Excel spreadsheet")]
    public class FactorFromFile: Model, IReferenceExternalFiles, IGenerateNodes, ILineEditor
    {
        /// <summary>
        /// The types of columns within the CSV, used to help determine how to read the inputs.
        /// </summary>
        private enum CommandType { Replacement, Set, Label, None };

        private string _filename { get; set; } = null;

        private DateTime _fileUpdated { get; set; } = DateTime.MinValue;

        private DataTable _data { get; set; } = null;

        /// <summary>
        /// Stores the name of the previous factor name in case its changed and we need to clean it up.
        /// </summary>
        private string _previousFactorName = "";

        /// <summary>
        /// The name of the Excel spreadsheet containing all factors details. 
        /// One sheet (Properties) contains the label and full APSIM Node path 
        /// for any properties used. Each other spreadsheet if a factor using 
        /// the sheet name with the first column (levels) containing the factor 
        /// levels and each column after that representing the values to set 
        /// for a property identified by the name of the column matching the 
        /// property label in the Properties sheet. 
        /// </summary>
        [Description("Factor details spreadsheet")]
        [Display(Type = DisplayType.FileName)]
        public string FileName { 
            get 
            {
                string apsimFilePath = "";
                if (Node != null)
                    apsimFilePath = Node.FileName;
                else
                {
                    Simulations sims = Node.FindParent<Simulations>(recurse: true);
                    if (sims != null)
                        apsimFilePath = sims.FileName;
                }
                if (string.IsNullOrEmpty(apsimFilePath))
                    throw new Exception("Cannot determine weather file path: Weather model is not attached to a simulation node or simulation. Please ensure the weather model is correctly attached to a simulation node.");
                else
                    return PathUtilities.GetRelativePath(_filename, apsimFilePath);
            } 
            set
            {
                _filename = value;
                if (!string.IsNullOrEmpty(_filename))
                    _fileUpdated = File.GetLastWriteTime(value);
            }
        }

        /// <summary>
        /// The name of the factor that will be built
        /// </summary>
        [Description("Factor Name")]
        public string FactorName { get; set; }

        /// <summary>
        /// The column in the data table that is used to label the composite 
        /// factor that is built from that row.
        /// </summary>
        [Description("Label Column")]
        public string LabelColumn { get; set; }

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

        /// <summary>Contents of the CSV file</summary>
        [Display]
        public DataTable Data
        {
            get {
                if (_data != null)
                    return _data;
                else
                    return new DataTable();
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
            get {return GetCommands();} 
            set {return;}
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            FileName = Path.GetFileName(FileName);
        }

        /// <summary>
        /// Returns true if the file has been updated more recently than the last read of the file.
        /// </summary>
        public bool CheckFileUpdated()
        {
            if (string.IsNullOrEmpty(_filename))
                return false;

            if (_fileUpdated != File.GetLastWriteTime(FullFileName))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public bool GenerateNodes()
        {
            string relativeDirectory = Path.GetDirectoryName(Node.FileName);
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(FactorName) || string.IsNullOrEmpty(LabelColumn))
                return false;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(GetCommands(), experiment, relativeDirectory);
                CommandProcessor.Run(commands, experiment, runner: null);
            }
            return true;
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public bool CleanNodes()
        {
            string relativeDirectory = Path.GetDirectoryName(Node.FileName);
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(FactorName) || string.IsNullOrEmpty(LabelColumn))
                return false;

            if (string.IsNullOrEmpty(_previousFactorName))
                _previousFactorName = FactorName;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                Factor factor = experiment.Node.FindChild<Factor>(name: _previousFactorName, recurse: true);
                if (factor != null && factor.ReadOnly == true)
                {
                    IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(new List<string>() {$"delete [Factor] name {_previousFactorName}"}, experiment, relativeDirectory);
                    CommandProcessor.Run(commands, experiment, runner: null);
                }
            }
            _previousFactorName = FactorName;
            return true;
        }

        /// <summary>
        /// Method to read factors from an excel spreadsheet and populate parent model with factors and composite factor components.
        /// </summary>
        public List<string> GetCommands()
        {
            if (string.IsNullOrEmpty(FileName))
                return new List<string>();

            if (string.IsNullOrEmpty(FactorName))
                return new List<string>();

            if (string.IsNullOrEmpty(LabelColumn))
                return new List<string>();

            Factors factors = Node.FindParent<Factors>();
            if (factors == null)
                throw new Exception("FactorsFromFile cannot find parent Factors");

            Experiment experiment = factors.Node.FindParent<Experiment>();
            if (experiment == null)
                throw new Exception("FactorsFromFile cannot find Experiment");

            using StringWriter log = new StringWriter();
            log.WriteLine($"Experiment factors imported from {FullFileName}");

            _data = FileUtilities.ReadDataFile(FullFileName);
            _fileUpdated = File.GetLastWriteTime(FullFileName);
            List<CommandType> columnCommandType = new List<CommandType>();
            foreach(DataColumn column in _data.Columns)
            {
                string header = column.ColumnName.Trim();
                if (header == LabelColumn)
                {
                    columnCommandType.Add(CommandType.Label);
                }
                else
                {
                    if (header.Contains('[') && header.Contains(']'))
                        header = header.Replace("[", "").Replace("]", "");
                    VariableComposite variable = ColumnInfo.NameMatchesAPSIMModel(header, experiment);
                    if (variable == null)
                        columnCommandType.Add(CommandType.None);
                    else if (typeof(IModel).IsAssignableFrom(variable.DataType))
                        columnCommandType.Add(CommandType.Replacement);
                    else
                        columnCommandType.Add(CommandType.Set);
                }
            }

            List<string> commands = new List<string>();
            commands.Add($"add new Factor to [{factors.Name}] name {FactorName}");
            foreach(DataRow row in _data.Rows)
            {
                string label = row[LabelColumn].ToString().Trim();
                commands.Add($"add new CompositeFactor to [{factors.Name}].{FactorName} name {label}");
                for(int i = 0; i < _data.Columns.Count; i++)
                {
                    DataColumn column = _data.Columns[i];
                    CommandType commandType = columnCommandType[i];
                    string columnName = column.ColumnName.Trim();
                    string value = row[columnName].ToString().Trim();

                    if (columnName.StartsWith('[') && columnName.EndsWith(']'))
                        columnName = columnName.Substring(1, columnName.Length-2);
                    if (commandType == CommandType.Replacement)
                    {
                        if (value.Contains(" from "))
                        {
                            int index = value.IndexOf(" from ");
                            string modelToFetch = value.Substring(0, index).Trim();
                            if (!modelToFetch.StartsWith('[') && !modelToFetch.EndsWith(']'))
                                value = "[" + modelToFetch + "]" + value.Substring(index);
                            commands.Add($"add {value} to [{factors.Name}].{FactorName}.{label}");
                        }
                        else
                            commands.Add($"add new {value} to [{factors.Name}].{FactorName}.{label}");
                        commands.Add($"[{factors.Name}].{FactorName}.{label}.Specifications += [{columnName}]");
                    }
                    else if (commandType == CommandType.Set)
                    {
                        commands.Add($"[{factors.Name}].{FactorName}.{label}.Specifications += {column}={value}");
                    }
                }
                commands.Add($"[{factors.Name}].{FactorName}.{label}.ReadOnly = true");
            }
            commands.Add($"[{factors.Name}].{FactorName}.ReadOnly = true");

            return commands;
        }
    }
}
