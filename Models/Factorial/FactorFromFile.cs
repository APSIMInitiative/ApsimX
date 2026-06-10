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
    /// ## FactorFromFile
    /// 
    /// The FactorFromFile model allows the loading of a table of simulation modifications from a csv or excel file. 
    /// This model is the first of its kind in APSIM and will generate a node structure within your file based on the 
    /// input it is given. This allows the construction of complex experiment configurations, without needing to build 
    /// it by hand within the APSIM interface.
    /// 
    /// ### Inputs
    /// - Filepath to your file (.csv or .xlsx)
    /// - Name of the sheet (if using .xlsx)
    /// - Name of the column for labelling your simulations
    /// 
    /// ### Stucture of File
    /// 
    /// The structure of your table can follow the example below. Where one column acts as the name of that simulation 
    /// ( **SimName** ), and that column name is given to the model in the interface. 
    /// 
    /// Columns that are text only are used as Simulation decriptors ( **ColumnLabel** ) and will be saved in the 
    /// datastore and usable in graphing and other tools as filter options. 
    /// 
    /// To do a replacement of a model with another model ( **[ModelToReplace]** ) provide an APSIM link as the column 
    /// name, and then another model link for each row in that column. This can also be used to copy a model from 
    /// another apsim file, however that process can be slow if your experiment is very large.
    /// 
    /// To set a property on a model (**[Model].Property**), create an apsim link to the property as the column name 
    /// and add a value in for each row. This value can be a number (with or without decimal places), text or an array 
    /// of values (seperated by commas). Arrays should be bounded by quotations if using a csv file, as the commas need 
    /// to be escaped.
    /// 
    /// Lastly for properties on a manager script ( **[Manager].Script.Property** ) the additional .Script. entry must 
    /// be provided to reference the properties in the script. This is similar to other models in APSIM such as reports 
    /// and operations. References to models can be passed in here as well by providing an APSIM link.
    /// 
    /// | SimName | ColumnLabel | [ModelToReplace]                             | [Model].Property | [Manager].Script.Property |
    /// |---------|-------------|----------------------------------------------|------------------|---------------------------|
    /// | Sim1    | First       | [Folder].ModelToCopy                         | 10               | 20                        |
    /// | Sim2    | Second      | [Folder].ModelToCopy from anotherfile.apsimx | 20               | [SomeModelReference]      |
    /// 
    /// This table is converted to a list of APSIM commands that describe everything that must be done to the file in 
    /// order to create the simulation modifications that you've provided. These commands are run whenever the file is 
    /// loaded, run or the FactorFromFile is refreshed, and the currnetly open file is modified with read-only nodes. 
    /// These generated nodes are not saved into your file, and are recreated whenever the file is openned again.
    /// 
    /// ```
    /// add new CompositeFactor to [Factors].MyFactor name Sim1
    /// [Factors].MyFactor.Sim1.DescriptorNames += ColumnLabel
    /// [Factors].MyFactor.Sim1.DescriptorValues += First
    /// [Factors].MyFactor.Sim1.Specifications += [ModelToReplace]
    /// [Factors].MyFactor.Sim1.Specifications += [Model].Property=10
    /// [Factors].MyFactor.Sim1.Specifications += [Manager].Script.Property=20
    /// [Factors].MyFactor.Sim1.ReadOnly = true
    /// add new CompositeFactor to [Factors].MyFactor name Sim2
    /// [Factors].MyFactor.Sim2.DescriptorNames += ColumnLabel
    /// [Factors].MyFactor.Sim2.DescriptorValues += Second
    /// [Factors].MyFactor.Sim2.Specifications += [Folder].ModelToCopy from anotherfile.apsimx
    /// [Factors].MyFactor.Sim2.Specifications += [Model].Property=20
    /// [Factors].MyFactor.Sim2.Specifications += [Manager].Script.Property=[SomeModelReference]
    /// [Factors].MyFactor.Sim2.ReadOnly = true
    /// ```
    /// 
    /// In the end, you would end up with a file that looks something like this:
    /// 
    /// ```
    /// Experiment
    /// - Folder
    /// - - ModelToCopy
    /// - Factors
    /// - - MyFactor (FactorFromFile)
    /// - - - Sim1 (CompositeFactor)
    /// - - - - ModelToCopy
    /// - - - Sim2 (CompositeFactor)
    /// - - - - ModelToCopy (from anotherfile.apsimx)
    /// - Simulation
    /// ```
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
        private enum CommandType { None, Label, Replacement, Set, SetDate, Descriptor };

        private string _filename { get; set; } = null;

        private DataTable _data { get; set; } = null;

        private string[] _commands { get; set; } = new string[0];

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
        [Description("Sheet")]
        public string Sheet { get; set; }

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
            if (string.IsNullOrEmpty(relativeDirectory) || string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(NameColumn))
                return false;

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment != null)
            {
                bool readOnly = ReadOnly;
                try
                {
                    //Check if this model is read only, and disable temporarily while generating the children
                    if (readOnly)
                        ReadOnly = false;

                    _commands = GetCommands().ToArray();
                    IEnumerable<IModelCommand> commands = CommandLanguage.StringToCommands(_commands, experiment, relativeDirectory);
                    CommandProcessor.Run(commands, experiment, runner: null);
                }
                catch (Exception exception)
                {
                    CleanNodes();
                    throw new Exception(exception.Message);
                }
                finally //reset the read only status
                {
                    ReadOnly = readOnly;
                }
            }

            return true;
        }

        /// <summary>
        /// Create the nodes
        /// </summary>
        public bool CleanNodes()
        {
            if (string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(NameColumn))
                return false;

            List<string> commands = new List<string>();
            foreach(IModel child in Children)
                commands.Add($"delete [{Name}].{child.Name}");

            if (commands.Count > 0)
            {
                try
                {
                    CommandProcessor.Run(CommandLanguage.StringToCommands(commands, this, null), this, runner: null);
                }
                catch {}
            }

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

            Experiment experiment = Node.FindParent<Experiment>(recurse:true);
            if (experiment == null)
                return ["### FactorFromFile cannot find Experiment ###"];

            Simulation simulation = experiment.Node.FindChild<Simulation>();
            if (simulation == null)
                return ["### FactorFromFile cannot find Simulation ###"];

            try
            {
                _data = FileUtilities.ReadDataFile(FullFileName, Sheet);
                foreach(DataColumn column in _data.Columns)
                    column.ReadOnly = true;
            }
            catch (Exception exception)
            {
                return ["### Error Reading input file ###", 
                        "### " + exception.Message + " ###"];
            }

            if (!_data.GetColumnNames().Contains(NameColumn))
                return [$"### Sheet \"{Sheet}\" does not have a column called \"{NameColumn}\" ###"];

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
                    {
                        bool hasSymbols = header.Any(c => !char.IsLetterOrDigit(c));
                        if (hasSymbols)
                            columnCommandType.Add(CommandType.None);
                        else
                            columnCommandType.Add(CommandType.Descriptor);
                    }
                    else if (typeof(IModel).IsAssignableFrom(variable.DataType) && variable.Property == null)
                    {
                        columnCommandType.Add(CommandType.Replacement);
                    }
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
            foreach(DataRow row in _data.Rows)
            {
                string label = row[NameColumn].ToString().Trim();
                commands.Add($"add new CompositeFactor to [{parent.Name}].{Name} name {label}");
                for(int i = 0; i < _data.Columns.Count; i++)
                {
                    DataColumn column = _data.Columns[i];
                    CommandType commandType = columnCommandType[i];
                    string columnName = column.ColumnName.Trim();
                    string value = row[columnName].ToString().Trim();
                    if (row[columnName] is DateTime date)
                        value = DateUtilities.GetDateAsString(date);
                    if (value.StartsWith('"') && value.EndsWith('"') && value.Contains(','))
                        value = value.Substring(1, value.Length-2);

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
                                commands.Add($"add {value} to [{parent.Name}].{Name}.{label}");
                            }
                            else if (value.StartsWith('[') && value.Contains(']') || value.StartsWith('.'))
                            {
                                commands.Add($"add {value} to [{parent.Name}].{Name}.{label}");
                            }
                            else
                                commands.Add($"add new {value} to [{parent.Name}].{Name}.{label}");
                            commands.Add($"[{parent.Name}].{Name}.{label}.Specifications += [{columnName}]");
                        }
                        else if (commandType == CommandType.SetDate)
                        {
                            string dateString = DateUtilities.GetDateAsString(DateUtilities.GetDate(row[columnName].ToString()));
                            commands.Add($"[{parent.Name}].{Name}.{label}.Specifications += {column}={dateString}");
                        }
                        else if (commandType == CommandType.Set)
                        {
                            commands.Add($"[{parent.Name}].{Name}.{label}.Specifications += {column}={value}");
                        }
                        else if (commandType == CommandType.Descriptor)
                        {
                            commands.Add($"[{parent.Name}].{Name}.{label}.DescriptorNames += {column}");
                            commands.Add($"[{parent.Name}].{Name}.{label}.DescriptorValues += {value}");
                        }
                    }
                }
                commands.Add($"[{parent.Name}].{Name}.{label}.ReadOnly = true");
            }
            return commands.ToArray();
        }

        /// <summary>
        /// Return all possible factor values for this factor.
        /// </summary>
        public List<CompositeFactor> GetCompositeFactors()
        {
            return Node.FindChildren<CompositeFactor>().Where(f => f.Enabled).ToList();
        }
    }
}
