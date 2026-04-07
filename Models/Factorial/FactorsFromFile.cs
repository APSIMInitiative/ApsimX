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
    [Description("Generate factors as specified in an Excel spreadsheet")]
    public class FactorsFromFile: Model, IReferenceExternalFiles, IGenerateNodes
    {
        private enum CommandType { Replacement, Set, Label, None };

        private string _filename { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private DateTime _fileUpdated { get; set; } = DateTime.MinValue;

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
            get {return _filename;} 
            set
            {
                _filename = value;
                _fileUpdated = File.GetLastWriteTime(value);
            }
        }

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

        /// <summary></summary>
        [Display]
        public DataTable Data
        {
            get;
            set;
        }

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FullFileName };
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            FileName = Path.GetFileName(FileName);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CheckFileUpdated()
        {
            DateTime time = File.GetLastWriteTime(FileName);
            if (_fileUpdated != time)
            {
                _fileUpdated = time;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public void GenerateNodes(string relativeDirectory)
        {
            CleanNodes(relativeDirectory);
            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(GetCommands(), experiment, relativeDirectory);
                CommandProcessor.Run(commands, experiment, runner: null);
            }
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public void CleanNodes(string relativeDirectory)
        {
            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                Factor factor = experiment.Node.FindChild<Factor>(name: FactorName, recurse: true);
                if (factor != null && factor.ReadOnly == true)
                {
                    IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(new List<string>() {$"delete [Factor] name {FactorName}"}, experiment, relativeDirectory);
                    CommandProcessor.Run(commands, experiment, runner: null);
                }
            }
        }

        /// <summary>
        /// Method to read factors from an excel spreadsheet and populate parent model with factors and composite factor components.
        /// </summary>
        public List<string> GetCommands()
        {
            Factors factors = Node.FindParent<Factors>();
            if (factors == null)
                throw new Exception("FactorsFromFile cannot find parent Factors");

            Experiment experiment = factors.Node.FindParent<Experiment>();
            if (experiment == null)
                throw new Exception("FactorsFromFile cannot find Experiment");

            using StringWriter log = new StringWriter();
            log.WriteLine($"Experiment factors imported from {FullFileName}");

            Data = FileUtilities.ReadDataFile(FullFileName);
            List<CommandType> columnCommandType = new List<CommandType>();
            foreach(DataColumn column in Data.Columns)
            {
                string header = column.ColumnName.Trim();
                if (header == LabelColumn)
                {
                    columnCommandType.Add(CommandType.Label);
                }
                else
                {
                    if (header.StartsWith('[') && header.EndsWith(']'))
                        header = header.Substring(1, header.Length-2).Trim();
                    VariableComposite variable = ColumnInfo.NameMatchesAPSIMModel(header, experiment);
                    if (variable == null)
                        columnCommandType.Add(CommandType.Set);
                    else if (typeof(IModel).IsAssignableFrom(variable.DataType))
                        columnCommandType.Add(CommandType.Replacement);
                    else
                        columnCommandType.Add(CommandType.Set);
                }
            }

            List<string> commands = new List<string>();
            commands.Add($"add new Factor to [{factors.Name}] name {FactorName}");
            foreach(DataRow row in Data.Rows)
            {
                string label = LabelColumn + row[LabelColumn].ToString();
                commands.Add($"add new CompositeFactor to [{factors.Name}].{FactorName} name {label}");
                for(int i = 0; i < Data.Columns.Count; i++)
                {
                    DataColumn column = Data.Columns[i];
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

            // I'm not sure how to present this information to the user.
            // summary hasn't been created and we don't want to write for each simulation in experiment, but once at start of simulation, but I couldn't find a suitable event.
            // is there a way to ask if this is the first of a group of simulations?

            // The components are actually added to the simulation tree as this is not a copy of base simulation as is used in the simulations.
            // This is good as from this point onward the factors appear as if the user build them and will be overwritten each time the simulation runs based on the spreadsheet.
            // The latest approach is to create the components as children below this component so they can been inspected by user and it is clear they are associated with the loading of factors from file.
            // This required these to be included in factors identified below an experiment or permutation as they would otherwise be hidden  one level deeper so some recurse:true settings are needed.

            // Is there a way to fire the building of the UI tree after this method so that the new components are visible in the UI immediately?
            // This should not be possible as UI is not the concern of the Model, but the UI has no say over the execution of Experiments.
            // If not, they only become visible upon opening the next instance of APISM, but are still present for the correct execution of the simulations.

            // If we can't display this, all StringWriter code can be deleted.
            // Otherwise it would need a special presenter with the property entry area and a text/markdown display section for the last genrated details information string.
            // Summary.WriteMessage(this, log.ToString(), MessageType.Information);
        }
    }
}
