using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PreSimulationTools.ObservationsInfo;
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
    [Description("Generate factors as specified in an Excel spreadsheet or csv file")]
    public class FactorFromFile: Model, IReferenceExternalFiles, IGenerateNodes, ILineEditor
    {
        /// <summary>
        /// The types of columns within the CSV, used to help determine how to read the inputs.
        /// </summary>
        private enum CommandType { Replacement, Set, SetDate, Label, None };

        private string _filename { get; set; } = null;

        private DataTable _data { get; set; } = null;

        private string[] _commands { get; set; } = new string[0];

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
            }
        }

        /// <summary>
        /// The column in the data table that is used to label the composite 
        /// factor that is built from that row.
        /// </summary>
        [Description("Factor / Sheet")]
        public string Factor { get; set; }

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
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(Factor) || string.IsNullOrEmpty(NameColumn))
                return false;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                _commands = GetCommands().ToArray();
                IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(_commands, experiment, relativeDirectory);
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
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(Factor) || string.IsNullOrEmpty(NameColumn))
                return false;

            if (string.IsNullOrEmpty(_previousFactorName))
                _previousFactorName = Factor;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                Factor factor = experiment.Node.FindChild<Factor>(name: _previousFactorName, recurse: true);
                if (factor != null && factor.ReadOnly == true)
                {
                    IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(new List<string>() {$"delete [{factor.Name}]"}, experiment, relativeDirectory);
                    CommandProcessor.Run(commands, experiment, runner: null);
                }
            }
            _previousFactorName = Factor;
            return true;
        }

        /// <summary>
        /// Method to read factors from an excel spreadsheet and populate parent model with factors and composite factor components.
        /// </summary>
        private string[] GetCommands()
        {
            if (string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(Factor) || string.IsNullOrEmpty(NameColumn))
                return new string[0];

            IModel parent = Node.FindParent<IModel>();
            if (parent == null)
                return ["### FactorFromFile cannot find parent ###"];

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment == null)
                return ["### FactorFromFile cannot find Experiment ###"];

            Simulation simulation = experiment.Node.FindChild<Simulation>();
            if (simulation == null)
                return ["### FactorFromFile cannot find Simulation ###"];

            try
            {
                _data = FileUtilities.ReadDataFile(FullFileName, Factor);
            }
            catch (Exception exception)
            {
                return ["### Error Reading input file ###", 
                        "### " + exception.Message + " ###"];
            }

            if (!_data.GetColumnNames().Contains(NameColumn))
                return new string[0];

            List<CommandType> columnCommandType = new List<CommandType>();
            foreach(DataColumn column in _data.Columns)
            {
                string header = column.ColumnName.Trim();
                if (header == NameColumn)
                {
                    columnCommandType.Add(CommandType.Label);
                }
                else
                {
                    VariableComposite variable = Node.GetObject(header, LocatorFlags.None, simulation);
                    if (variable == null)
                        columnCommandType.Add(CommandType.None);
                    else if (typeof(IModel).IsAssignableFrom(variable.DataType))
                        columnCommandType.Add(CommandType.Replacement);
                    else
                    {
                        if (variable.DataType == typeof(DateTime))
                            columnCommandType.Add(CommandType.SetDate);
                        else
                            columnCommandType.Add(CommandType.Set);
                    }
                        
                }
            }

            List<string> commands = new List<string>();
            commands.Add($"add new Factor to [{parent.Name}] name {Factor}");
            commands.Add($"move [{Factor}] after [{Name}]");
            foreach(DataRow row in _data.Rows)
            {
                string label = row[NameColumn].ToString().Trim();
                commands.Add($"add new CompositeFactor to [{parent.Name}].{Factor} name {label}");
                for(int i = 0; i < _data.Columns.Count; i++)
                {
                    DataColumn column = _data.Columns[i];
                    CommandType commandType = columnCommandType[i];
                    string columnName = column.ColumnName.Trim();
                    string value = row[columnName].ToString().Trim();
                    if (row[columnName] is DateTime date)
                        value = DateUtilities.GetDateAsString(date);

                    if (!string.IsNullOrEmpty(value))
                    {
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
                                commands.Add($"add {value} to [{parent.Name}].{Factor}.{label}");
                            }
                            else
                                commands.Add($"add new {value} to [{parent.Name}].{Factor}.{label}");
                            commands.Add($"[{parent.Name}].{Factor}.{label}.Specifications += [{columnName}]");
                        }
                        else if (commandType == CommandType.SetDate)
                        {
                            string dateString = DateUtilities.GetDateAsString(DateUtilities.GetDate(row[columnName].ToString()));
                            commands.Add($"[{parent.Name}].{Factor}.{label}.Specifications += {column}={dateString}");
                        }
                        else if (commandType == CommandType.Set)
                        {
                            commands.Add($"[{parent.Name}].{Factor}.{label}.Specifications += {column}={value}");
                        }
                    }
                }
                commands.Add($"[{parent.Name}].{Factor}.{label}.ReadOnly = true");
            }
            commands.Add($"[{parent.Name}].{Factor}.ReadOnly = true");

            return commands.ToArray();
        }
    }
}
